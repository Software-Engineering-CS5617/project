## Features 	



- **Automated Image Diffing:** The captured frame is divided into fixed-size tiles. Each tile is assigned a hash value and compared with the corresponding tile from the previous frame. Only changed tiles are transmitted, while unchanged tiles remain cached at the receiver. If no tiles change, a lightweight pulse/heartbeat signal is transmitted instead of image data.
- **Tile-Based Processing:** The screen is divided into small 64×64 tiles so that only the areas that change need to be processed.
- **Hash-Based Change Detection:** Each tile gets a small hash value, which is compared with its previous value to quickly detect changes.
- **Full-Frame Fallback:** If too much of the screen changes, the system sends the complete frame instead of many individual tiles.
- **Static Screen Optimization:** If nothing changes on the screen, the system sends only a small pulse instead of sending the same image again.
- **Frame Synchronization:** Each frame is given a frame number so the receiver can correctly identify and arrange incoming tiles.
- **Client-Side Stitching:** The receiver keeps the previous frame and replaces only the tiles that have changed.
- **Selective Fetching:** Streams that are not currently visible can be stopped temporarily to save bandwidth
- **Resolution Handling:** The system can handle different screen resolutions and scale the received frame appropriately.
- **Adaptive Resolution:** When the network is slow, the resolution can be reduced to maintain smoother streaming.
- **Packet-Based Communication:** Image data and control information are separated into different types of packets for easier handling.
- **Grid UI:** Displays feeds as tiles in a XAML Grid, featuring click-to-zoom functionality for focused viewing.
- **Incident Snapshot Menu:** Includes an event listener for a right-click context menu, enabling a "Snip" command to instantly freeze and capture the rendered frame.
- **Targeted Rendering Framerate :&#x20;**&#x32;4 to 30 FPS 
- **Image resolution :&#x20;**&#x37;20p 
  - Can be extended to fallback to 480p or lower resolution if network is slow
- **Communication :&#x20;**&#x45;ither a peer to peer model or clustering\*.

## Architecture & Workflow

### 1. Data Capture & Processing (Sender)

A C# API retrieves the current state of the screen or camera feed at a set interval. The diffing algorithm processes the raw visual data. If changes are detected, altered pixels are isolated, converted into a standardized format, and serialized into strings. If no changes are detected, a static pulse is generated. The prepared object is then handed off to the network module for transmission.

### 2. Network Reception & Rendering (Receiver)

The network module continuously listens for incoming data objects. Upon receiving a pulse, the UI maintains the preivous frame. Upon receiving delta change, the Image Stitcher reconstructs the updated frame. The final images (reconstructed/cached frames) are pushed to theUI, automatically generating scrollbars for overflow and actively managing which streams are fetched based on current visibility.

### 3. Incident API Integration

When a snapshot is triggered via the context menu, the system isolates the current stitched frame in memory. The raw data is converted to a PNG, serialized into a string, and tagged with precise metadata (timestamp and stream ID). This payload is transmitted to the Incident Manager API.

## Data Structures

### Network Transmission Payload

This class packages pixel updates efficiently before network broadcast.



| **Field**        | **Type**       | **Description**                                           |
| ---------------- | -------------- | --------------------------------------------------------- |
| **StreamID**     | String         | Identifies the screen/camera stream                       |
| **FrameNumber**  | Integer/Long   | Unique identifier for the frame                           |
| **Timestamp**    | DateTime       | Time at which the frame was captured                      |
| **IsPulse**      | Boolean        | Indicates whether this packet is a pulse                  |
| **IsFullFrame**  | Boolean        | Indicates whether the complete frame is being transmitted |
| **Resolution**   | Width × Height | Source frame resolution                                   |
| **TileSize**     | Integer        | Size of each tile, currently 64×64                        |
| **ChangedTiles** | List           | Coordinates and image data of changed tiles               |
| **ImageData**    | Byte[]/String  | Complete frame data when full-frame fallback is used      |



### Tile Packet Structure



| **Field**       | **Type**     | **Description**                 |
| --------------- | ------------ | ------------------------------- |
| **StreamID**    | String       | Source stream                   |
| **FrameNumber** | Integer/Long | Frame to which the tile belongs |
| **TileX**       | Integer      | Horizontal tile index           |
| **TileY**       | Integer      | Vertical tile index             |
| **Width**       | Integer      | Tile width                      |
| **Height**      | Integer      | Tile height                     |
| **TileData**    | Byte[]       | Encoded image data              |





### 4. Load Management



A standard client-server model is likely to overload the server for the ScreenShare module. This implementation is however done as a fallback in case the new approach fails.



**Client-Server Model**



- The ‘server’ is the machine of the user that starts the meeting (the meeting host).
- A user’s screenshare is shared directly with the host.
- From here the host/server shares all screenshares with requesting clients
- A client request multiple screenshares in tiled view
- Only the required screenshare is fetched when it is in focus view.



**Load Balancing**



- The host maintains a send_to list for each user sharing their screen
- The users also maintain their own copies of the send_to list
- A user sends their screenshare to all users in the send_to list
- The list whenever a receiver wants to receive/block a screenshare of a particular user, they send an appropriate request to the host which then updates the send_to list of the necessary sharers.
- This is essentially a peer to peer protocol facilitated by the host.
- Uses multicast to minimize load on the sharer.



**Edge Conditions**



-  The client server is limited by the capacity of the server. It bottlenecks on increasing the number of concurrent streams as well as the number of viewers. However it is simpler to implement.
- The peer to peer model is limited only by the number of viewers of a particular stream and only affects that stream since the load is distributed.
- Operating under the assumption that when the host exits the stream ends.
- Default is the peer to peer model but if an individual stream is bottlenecked, the host can act as a relay to distribute the load.








