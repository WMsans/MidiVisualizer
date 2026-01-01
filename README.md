# MIDI Visualizer: Sequence Frame Generator

This program generates a sequence of visual frames based on a MIDI file.
How to Use
1. Download and Unzip the package.
2. Double-click MidiVisualizer.exe to run.
3. Enter your desired parameters when prompted.
4. A new folder containing the image sequence will be generated in the same directory as the executable.

### Parameter Configuration

```MIDI File Path``` The path to the MIDI folder or file accessible by the program. Quotation marks are optional.

```BPM``` The program automatically parses the BPM from the MIDI file by default. However, you can enter a custom value to override it.

```Canvas Width``` The horizontal size of the generated frames (in pixels).

```Canvas Height``` The vertical size of the generated frames (in pixels).

```Judgment Line``` X-Coordinate The distance of the judgment line from the left edge of the canvas (in pixels).

```Judgment Line Width``` The thickness (horizontal width) of the judgment line (in pixels).

```Note Height``` The vertical size (thickness) of the notes (in pixels).

```Note Spacing``` The distance between adjacent notes in pixels. Set to 0 for no gap.

```Initial Rotation Angle``` The angle (in degrees) at which a note rotates when it first hits the judgment line.

```Initial Rotation Mode``` Determines how the note rotates upon contact:
1. Dynamic Adjustment: Longer notes rotate less (as angular changes are more visually obvious on long objects).
2. Fixed Angle: Every note rotates by a fixed, constant angle.

```Initial Shake Mode``` Determines how the note shakes upon contact:
1. Vibration: Direction changes constantly.
2. Unidirectional: Shakes in a single direction only.

```Recovery Time``` The duration (in seconds) it takes for the animation to fade or reset after the note hits the line.

```Shake Amplitude``` The range (in pixels) of the random jitter when a note is in the active state.

```Shake Variance``` The random shaking follows a Normal Distribution. This value represents the variance. Larger values make the movement closer to a Uniform Distribution.

```Initial Shake Percentage``` The ratio of the initial shake distance (pixels) relative to the note's width.

```Scroll Speed (Flow Rate)``` The number of pixels the notes move per second.

```Video Frame Rate``` The target FPS (Frames Per Second) for the output sequence.

```Active Note Color``` The color of notes that have been activated. Supports Hex and RGB formats.

```Inactive Note Color``` The color of notes that have not yet been activated. Supports Hex and RGB formats.

```Background Color``` The color of the canvas background. Supports Hex and RGB formats.

```Judgment Line Color``` The color of the judgment line. Supports Hex and RGB formats.
