# Incident Management Design Doc 1
----
## What is an Incident?

An **Incident** contains the following details:

|Field|Type|Description|
|---|---|---|
|**Unique ID**|`ID`|Unique identifier for the incident|
|**Title**|`Text`|Title of the incident|
|**Description**|`Text`|Description of the incident|
|**Status**|`Enum`|`{pending, waiting for evidence, closed, ...}`|
|**Category**|`Enum`|`{drunk driving, theft, accident, ...}`|
|**Creation Time**|`Datetime`|Time at which the incident was created|
|**Update Time**|`Datetime`|Time at which the incident was last updated|
|**Evidence**|`List<Evidence>`|List of evidence associated with the incident|
|**Correlation Reprsentative**|`Incident_ID`| Incident ID which represents the correlated group of incidents. For more details read section on [Correlation](#correlation)|

### Evidence

An **Evidence** will be modelled as an **astract class** with following details:

|Field|Type|Description|
|---|---|---|
|**Description**|`Text`|Description of the evidence|
|**File Path**|`Optional<Text>`|Path to the evidence file; can be `None`. Supported file types: Images , Videos and PDFs
|**Upload Time**|`Datetime`|Time at which this evidence was uploaded |
|**Uploaded By**|`ID`|ID of the user who added this as evidence|

**Pseudo code for Evidence Class:**

```csharp
public abstract class Evidence {

    protected String description;
    protected Optional<String> filePath;
    protected DateTime uploadTime;
    protected ID uploadBy;

    protected Evidence(String description, Optional<String> filePath,ID userID) {
        this.description = description;
        this.filePath = filePath;
        this.uploadTime = DateTime.Now();
        this.uploadBy = userID;
    }

    public String getDescription() {
        return description;
    }

    public Optional<String> getFilePath() {
        return filePath;
    }

    public DateTime getUploadTime() {
        return uploadTime;
    }

    public ID getUploadBy() {
        return uploadBy;
    }
}
```

**Update/Modification Rules:**

The following fields in [Incident Class](#what-is-an-incident) are tentatively **not modifiable** it is created:
- Unique ID
- Title
- Description
- Creation Time

> [!Note]
> `Update Time` is updated automatically whenever a modification is made to the incident.

**Types of Evidence:**

We currently propose the following evidence/updates to a Incident.
- **File/Comment evidence**
    - Add a file as evidence.
    - Add a comment without an associated file.
- **Change category**
- **Change status**
- **Add an incident correlation**
    - Associate another incident with the current incident.

These updates will be stored in the `Evidence` field of [Incident](#what-is-an-incident) sorted by date of upload.

**Pseudo code for Evidence Class Types:**

1. **Normal File and Text Evidence**
```csharp
public class FileEvidence extends Evidence {

    public FileEvidence(String description, String filePath, ID userID) {
        super(description, Optional.of(filePath), userID);
    }
}
```

2. **Category Change**

```csharp
public class CategoryChange extends Evidence {

    public CategoryChange(String oldCategory, String newCategory, ID userID) {
        super(
            "Category changed from " + oldCategory + " to " + newCategory,
            Optional.empty(),
            userID
        );
    }
}
```

3. **Status Change**

```csharp
public class StatusChange extends Evidence {

    public StatusChange(String oldStatus, String newStatus, ID userID) {
        super(
            "Status changed from " + oldStatus + " to " + newStatus,
            Optional.empty(),
            userID
        );
    }
}

```
4. **Correlation**

```csharp
public class IncidentCorrelation extends Evidence {

    public IncidentCorrelation(String incidentId, ID userID) {
        super(
            "Incident correlated with " + incidentId,
            Optional.empty(),
            userID
        );
    }
}
```

---

## Incident Timeline

Incidents will be displayed as a **timeline**, showing each **evidence update** along with the user who performed it.

```
Incident Created by User X
        |
        +-- Status changed from Pending to Closed by User Y
        |
        +-- Category changed from Network to Security by User X
        |
        +-- Incident correlated with INC-123 by User Z

```

---


## Unique ID Generation

>[!Danger] Problem
Incidents can be created simultaneously by multiple devices in our LAN-based system. Since there is no centralized ID generator, we need a mechanism that can generate **globally unique IDs independently on each device** without collisions.

### **Solution**

We plan to use a **Snowflake-inspired ID generation** approach, originally developed by Twitter. A Snowflake ID combines a **timestamp, device/worker ID, and sequence number**, allowing multiple devices to generate unique, time-sortable IDs independently.

The ID follows a 64-bit Snowflake-style structure:

```text
┌───────┬───────────────────────────────┬──────────────┬─────────────────┐
│ 1 bit │          41 bits              │   10 bits    │     12 bits     │
│ Sign  │          Timestamp            │  Device ID   │    Sequence     │
│unused │   milliseconds since epoch    │              │ per millisecond │
└───────┴───────────────────────────────┴──────────────┴─────────────────┘
```

- **Timestamp (41 bits):** Provides time ordering and allows IDs to remain unique across time.
- **Device ID (10 bits):** Uniquely identifies the device generating the ID, supporting up to 1024 devices.
- **Sequence (12 bits):** Differentiates multiple incidents generated by the same device within the same millisecond, supporting up to 4096 IDs/ms per device.

>[!example] If two devices create an incident simultaneously
The different Device IDs ensure that the generated IDs do not collide.

### C# Implementation

>[!Note]
>Rather than implementing the algorithm ourselves, we will use an existing C#/.NET Snowflake library such as **IdGen**.

Example:

```csharp
using IdGen;

int deviceId = 12;
var generator = new IdGenerator(deviceId);

long incidentId = generator.CreateId();

Console.WriteLine(incidentId);
```

Each device will be configured with a **unique Device ID**. The generator then combines the device ID with the current timestamp and sequence number to produce the incident ID.

---

## Correlation

The correlation feature allows related or potentially duplicate incidents to be grouped together, allowing users to view incidents that belong to the same correlation group.

This module will support two methods of correlation:

- **Manual Correlation:** A user can manually select an incident and correlate it with the current incident.
- **AI-Assisted Correlation:** The system can use an AI API to identify incidents that may be related or duplicates of the current incident and assist the user in correlating them.

**Correlation Data Structure**
- To efficiently maintain correlation groups, we propose using a **Disjoint Set Union (DSU)** data structure also known as **Union-Find**.
- Each incident is represented by its unique incident ID.

**Initial State**
- When an incident is created, it initially forms its own correlation group. Therefore, every incident is initially its own representative.
- This indicates that none of the incidents are currently correlated with another incident.

**Correlating Incidents**
- When the module is asked to correlate incident X with incident Y, it first determines the representatives of both incidents using the `find` operation.
- If both incidents have the same representative, they are already part of the same correlation group.
- If they have different representatives, their correlation groups are merged using the `union` operation.

**Maintaining Group Members**
- DSU efficiently determines whether two incidents belong to the same group, but it does not directly provide a list of all incidents in a group.
- Therefore, we can maintain a Hash Map: [Correlation Representative](#what-is-an-incident) -> list of correlated incident IDs.
- When two different correlation groups are merged, their member lists are also merged.
- To keep merging efficient, union by size/rank and path compression can be used for the DSU structure. The member list of the smaller group can be merged into the larger group.


**Time Complexity**

> [!Note] Merging Correlated Groups
When two incidents are correlated, the DSU first determines the representatives of their respective groups.
With path compression and union by size/rank, the DSU find and union operations have an amortized time complexity of O(alpha(n)), which is effectively O(1) in practice.
However, the hash map storing the members of each correlation group must also be updated.
If the smaller group contains k incidents, merging its members into the larger group takes O(k) time.
Therefore, the overall cost of merging two correlation groups is O(k), where k is the size of the smaller group.

> [!Note] Retrieving Correlated Incidents
To retrieve the correlated incidents for a given incident:
> - Use the DSU to find the incident's correlation-group representative.
> - Use the representative as the key in the hash map to directly access the corresponding list of incidents.
Finding the group and accessing the hash-map entry are O(1) amortized.
If the group contains k incidents, returning the complete list takes O(k) time, since all k incident IDs must be returned.
Thus, there is no O(n) search across all incidents. The system only processes the incidents that actually belong to the requested correlation group.

**Correlation design summary**

- DSU to efficiently determine and merge correlation groups.
- Hash Map to maintain the list of incidents belonging to each correlation group.
- Path compression and union by size/rank to keep DSU operations efficient.

---
## Questions
> [!Question] **1. User ID during Incident Creation**
When creating an incident, a user ID is required to identify the user who created it.
>- Is the user ID provided by the Authentication Module / UX layer?
>- If so, should the module receive the authenticated user ID as part of the incident-creation request?

> [!Question] **2. Incident Storage and XML Processing**
Incidents are currently stored in XML format. We need to determine how incident data should be handled when creating or updating incidents.
>- How should the XML be parsed into the module's internal representation?
>- When an incident is created, updated or correlated, how should the modified incident data be written back to the XML?

> [!Question] **3. Hash Map**
Will the Hash map that maintains correlated groupd need to be stored in XML format along with the incidents?

> [!Question] **4. AI-assisted correlation**
How to expose AI-assisted correlation as a capability for the Insights team to handle?

---



## References

- [Snowflake Unique ID Twitter Repo](https://github.com/twitter-archive/snowflake/tree/b3f6a3c6ca8e1b6847baa6ff42bf72201e2c2231)
- [Snow flake ID wiki](https://en.wikipedia.org/wiki/Snowflake_ID)

---
