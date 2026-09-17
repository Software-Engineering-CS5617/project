# Features

- **Image Diffing:** Compares current screen frames against previously stored frames in local memory. Sends a lightweight "pulse" signal for static screens, or exact delta pixels for changed screens.
- **Full-Frame Fallback:** In situations involving a lot of motion (for example, when playing back live video) the diffing algorithm is bypassed and the entire image frames are processed if the number of pixel differences is large.
- **Client-Side Stitching:** Client-Side Stitching involves using a stitcher object to locally reconstruct the final images by placing the incoming delta pixels over the cached base frames.
- **Selective Fetching:** Signals the network to halt stream data transmission for feeds that are currently off-screen by the user.
- **Interactive Grid UI:** Displays feeds as tiles in a XAML Grid, featuring click-to-zoom functionality for focused viewing.
- **Incident Snapshot Menu:** Includes an event listener for a right-click context menu, enabling a "Snip" command to instantly freeze and capture the rendered frame.
- **Targeted Rendering Framerate:** 24 to 30 FPS
- **Image Resolution:** 720p
  - Can be extended to fall back to 480p or lower resolution if the network is slow.
- **Communication:** Either a peer-to-peer model or clustering*.

# Architecture & Workflow

## 1. Data Capture & Processing (Sender)

A C# API retrieves the current state of the screen or camera feed at a set interval. The diffing algorithm processes the raw visual data. If changes are detected, altered pixels are isolated, converted into a standardized format, and serialized into strings. If no changes are detected, a static pulse is generated. The prepared object is then handed off to the network module for transmission.

## 2. Network Reception & Rendering (Receiver)

The network module continuously listens for incoming data objects. Upon receiving a pulse, the UI maintains the cached frame. Upon receiving delta pixels, the Image Stitcher reconstructs the updated frame. The final images (reconstructed/cached frames) are pushed to the XAML StackPanel/Grid, automatically generating scrollbars for overflow and actively managing which streams are fetched based on current visibility.

## 3. Incident API Integration

When a snapshot is triggered via the context menu, the system isolates the current stitched frame in memory. The raw data is converted to a PNG, serialized into a string, and tagged with precise metadata (timestamp and stream ID). This payload is transmitted to the Incident Manager API.

# Data Structures

## Network Transmission Payload

This class packages pixel updates efficiently before network broadcast.

| **Field** | **Type** | **Description** |
|---|---|---|
| Timestamp | `DateTime` | The exact time the frame or pulse was captured. |
| IsPulse | `Boolean` | `true` if the screen is unchanged; `false` if there is pixel data. |
| Resolution | `String` | The source screen resolution for dynamic client-side scaling. |
| ChangedPixels | `Array/List` | The specific delta pixel data (coordinates and color values) if `IsPulse` is `false`. |
| ImageData | `String` | A string representation of the PNG snapshot for full frames. |

## Incident API Payload

This data object is transmitted to the Incident Manager API.

| **Field** | **Type** | **Description** |
|---|---|---|
| ImagePayload | `String` | The serialized PNG image data of the captured incident. |
| CaptureTime | `DateTime` | The exact local time the snapshot was triggered. |
| SourceStreamID | `String` | An identifier for the specific video/screen feed being monitored. |