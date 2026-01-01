using NAudio.Midi;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.Versioning;

namespace MidiVisualizer
{
    class Program
    {
        private static int DefaultCanvasWidth = 1920;
        private static int DefaultCanvasHeight = 1080;
        private static int DefaultGuideLineX = 960;
        private static int DefaultNoteDisplayHeight = 10;
        private static int DefaultNoteDistance = 0;
        private static double DefaultRotationAngle = 0; // Default rotation angle, in degrees
        private static double DefaultRotationManner = 0; // Default rotation mode, 0: Dynamic (longer note = smaller angle), 1: Fixed angle
        private static double DefaultShakeAmplitude = 10; // Default shake amplitude
        private static double DefaultShakeAmplitudeVariance = 0.0005; // Variance of shake amplitude
        public static double DefaultShakeActivation = 0; // Percentage of shake amplitude when note is first activated
        public static int DefaultShakeManner = 0; // Initial shake mode
        private static double DefaultReturnToCenterTime = 0.5; // Return to center time, in seconds
        private static double DefaultPixelsPerSecond = 1500; // 192.0 was the original default, 1 tick = 1 pixel default bpm
        private static int DefaultFps = 30;
        private static string DefaultMidiFilePath = "testttt.mid"; // Default MIDI file path
        private static Color DefaultActiveNoteColor = Color.White; // Default active note color
        private static Color DefaultInactiveNoteColor = Color.FromArgb(100, 100, 100); // Default inactive note color
        private static Color DefaultBackgroundColor = Color.Black; // Default background color
        private static Color DefaultGuidelineColor = Color.White; // Default guideline color
        private static int DefaultGuidelineWidth = 1; // Default guideline width

        private static double EaseOutCubic(double t) // Easing function
        {
            if (t < 0 || t > 1)
            {
                return 0;
            }
            return Math.Pow(1 - t, 3);
        }
        // Other easing functions:
        // EaseOutQuad: return 1 - (1 - t) * (1 - t);
        // EaseOutExpo: return (t == 1.0) ? 1.0 : 1 - Math.Pow(2, -10 * t);

        public static double UniformRandom(double range) // Uniform random number
        {
            return (_random.NextDouble() * 2 - 1) * range;
        }

        public static double UniformRandomExcludeMiddle(double a, double b) // Uniform random, excluding middle, range (-a,-b) U (b,a)
        {
            // Ensure positive numbers
            a = Math.Abs(a);
            b = Math.Abs(b);
            // Ensure a > b, swap if not
            if (a <= b)
            {
                double temp = a;
                a = b;
                b = temp;
            }
            double random = _random.NextDouble();

            if (random < 0.5) // 50% chance for left interval (-a, -b)
            {
                return -a + random * 2 * (a - b);
            }
            else // 50% chance for right interval (b, a)
            {
                return b + (random - 0.5) * 2 * (a - b);
            }
        }

        private static Random _random = new Random(); // Normal distribution random number
        public static double NormalRandom(double maxAmplitude, double variance)
        {
            double standardDeviation = Math.Sqrt(variance);

            // Use Central Limit Theorem, sum 12 uniform distributions to approximate normal distribution
            double sum = 0;
            for (int i = 0; i < 12; i++)
            {
                sum += _random.NextDouble();
            }

            // Standardize to mean 0, SD 1
            double standardNormal = sum - 6.0;

            // Adjust to specified standard deviation
            double result = standardNormal * standardDeviation;

            // Limit within max amplitude
            result = Math.Max(-maxAmplitude, Math.Min(maxAmplitude, result));

            return result;
        }

        static void ParseMidiFile(string filePath, List<Note> notes) // Parse MIDI file
        {
            var midiFile = new MidiFile(filePath);
            for (int track = 0; track < midiFile.Tracks; track++)
            {
                foreach (var midiEvent in midiFile.Events[track])
                {
                    // Check if it is NoteOnEvent and Velocity > 0 (Note On)
                    if (midiEvent is NoteOnEvent noteOn && noteOn.Velocity > 0)
                    {
                        //Console.Write($"Pitch: {noteOn.NoteNumber} ({noteOn.NoteName}) ");
                        //Console.Write($"Velocity: {noteOn.Velocity} ");
                        //Console.Write($"Channel: {noteOn.Channel} ");
                        //Console.Write($"Start: {noteOn.AbsoluteTime} ticks ");


                        // Get corresponding Note Off event
                        if (noteOn.OffEvent != null)
                        {
                            notes.Add(new Note(noteOn.AbsoluteTime, noteOn.OffEvent.AbsoluteTime, noteOn.NoteNumber, noteOn.NoteName));
                        }
                        else
                        {
                            // Handle cases with no matching OffEvent (e.g., corrupted MIDI or note not closed properly)
                            Console.WriteLine("End: No corresponding OffEvent found");
                        }
                    }
                }
            }
        }
        static double GetBpmFromMidiFile(string filePath)
        {
            var midiFile = new MidiFile(filePath);
            double bpm = 120.000; // Default value
            for (int track = 0; track < midiFile.Tracks; track++)
            {
                foreach (var midiEvent in midiFile.Events[track])
                {
                    if (midiEvent is TempoEvent tempoEvent)
                    {
                        return Math.Round(60000000.0 / tempoEvent.MicrosecondsPerQuarterNote, 3);
                    }
                }
            }
            return bpm;
        }
        static double GetDoubleInput(string prompt, double defaultValue)
        {
            Console.Write($"{prompt} (Default: {defaultValue}): ");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                return defaultValue;
            }
            if (double.TryParse(input, out double result))
            {
                return result;
            }
            Console.WriteLine("Invalid input, using default value.");
            return defaultValue;
        }
        static int GetIntInput(string prompt, int defaultValue)
        {
            Console.Write($"{prompt} (Default: {defaultValue}): ");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                return defaultValue;
            }
            if (int.TryParse(input, out int result))
            {
                return result;
            }
            Console.WriteLine("Invalid input, using default value.");
            return defaultValue;
        }
        static string GetStringInput(string prompt, string defaultValue)
        {
            Console.Write($"{prompt} (Default: {defaultValue}):");
            string? input = Console.ReadLine(); // Use nullable string?
            return string.IsNullOrWhiteSpace(input) ? defaultValue : input;
        }
        static Color GetColorInput(string prompt, Color defaultColor)
        {
            // Convert default color to readable string for user reference
            string defaultColorString = $"({defaultColor.R},{defaultColor.G},{defaultColor.B})";
            Console.Write($"{prompt} (Default: {defaultColorString}): ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                return defaultColor;
            }
            // Try parsing Hex (e.g. "#FF0000" or "FF0000")
            string hex = input.Trim();
            if (hex.StartsWith("#"))
            {
                hex = hex.Substring(1);
            }
            if (hex.Length == 6 && int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int hexValue))
            {
                try
                {
                    return Color.FromArgb(hexValue | (0xFF << 24)); // Ensure Alpha is 255 (Opaque)
                }
                catch { /* Parse failed */ }
            }
            string[] rgbParts = input.Split(',');
            if (rgbParts.Length == 3 &&
                int.TryParse(rgbParts[0].Trim(), out int r) && r >= 0 && r <= 255 &&
                int.TryParse(rgbParts[1].Trim(), out int g) && g >= 0 && g <= 255 &&
                int.TryParse(rgbParts[2].Trim(), out int b) && b >= 0 && b <= 255)
            {
                return Color.FromArgb(r, g, b);
            }
            Console.WriteLine("Invalid color input (please use Hex or R,G,B format), using default color.");
            return defaultColor;
        }
        [SupportedOSPlatform("windows6.1")]
        static void Main(string[] args)
        {
            Console.WriteLine
                (
                "===Midi Visualizer V1.1.0 (Video Export)===\n" +
                "Welcome to this tool\n" +
                "Press Enter to accept default values, or enter a new value\n" +
                "Note: Pay attention to the relation between note height and canvas height, notes might not fit vertically\n" +
                "Color input supports RGB and Hex\n" +
                "Currently does not support MIDI with tempo changes\n" +
                "Long press Enter to accept all defaults"
                );


            // --- Get User Input ---
            string filePath = GetStringInput("MIDI File Path", DefaultMidiFilePath);
            filePath = filePath.Trim('\"');

            var midiFile = new MidiFile(filePath);
            double bpm = 120.000;
            bpm = GetBpmFromMidiFile(filePath);

            double bpmInput = GetDoubleInput("BPM (Default uses internal MIDI value)", bpm);
            bpm = bpmInput;

            int canvasWidth = GetIntInput("Canvas Width (pixels)", DefaultCanvasWidth);
            int canvasHeight = GetIntInput("Canvas Height (pixels)", DefaultCanvasHeight);
            int LineEndX = GetIntInput("Guideline X Coordinate (pixels)", DefaultGuideLineX);
            int guideLineWidth = GetIntInput("Guideline Width (pixels, 0 = no render, visual only)", DefaultGuidelineWidth);
            int noteHeight = GetIntInput("Note Height (pixels)", DefaultNoteDisplayHeight);
            int noteDistance = GetIntInput("Note Spacing (pixels)", DefaultNoteDistance);
            double rotationAngle = -GetDoubleInput("Initial Rotation Angle", DefaultRotationAngle);
            double rotationManner = GetDoubleInput("Initial Rotation Mode (0: Dynamic, 1: Fixed)", DefaultRotationManner);
            int ShakeManner = GetIntInput("Initial Shake Mode (0: Vibrate, 1: One-way)", DefaultShakeManner);
            double returnToCenterTime = GetDoubleInput("Return to Center Time (seconds)", DefaultReturnToCenterTime);
            double shakeAmplitude = GetDoubleInput("Shake Amplitude (%, relative to note height)", DefaultShakeAmplitude);
            double shakeAmplitudeVariance = GetDoubleInput("Shake Amplitude Variance", DefaultShakeAmplitudeVariance);
            double shakeActivation = GetDoubleInput("Initial Shake Percentage (%, relative to note height)", DefaultShakeActivation);
            double pixelsPerSecond = GetDoubleInput("Scroll Speed (pixels/sec)", DefaultPixelsPerSecond);
            int fps = GetIntInput("Video Framerate (FPS)", DefaultFps);
            Color activeNoteColor = GetColorInput("Active Note Color", DefaultActiveNoteColor);
            Color inactiveNoteColor = GetColorInput("Inactive Note Color", DefaultInactiveNoteColor);
            Color backgroundColor = GetColorInput("Background Color", DefaultBackgroundColor);
            Color guidelineColor = GetColorInput("Guideline Color", DefaultGuidelineColor);



            Console.WriteLine("\n===Parameter Confirmation===");
            Console.WriteLine($"MIDI File: {filePath}");
            Console.WriteLine($"Canvas Size: {canvasWidth}x{canvasHeight}");
            Console.WriteLine($"Guideline X: {LineEndX}");
            Console.WriteLine($"Guideline Width: {guideLineWidth} ({(guideLineWidth > 0 ? "Will Render" : "No Render")})");
            Console.WriteLine($"Note Height: {noteHeight}");
            Console.WriteLine($"Pixels Per Second: {pixelsPerSecond}");
            Console.WriteLine($"Framerate: {fps}");
            Console.WriteLine($"Active Note Color: ({activeNoteColor.R},{activeNoteColor.G},{activeNoteColor.B})");
            Console.WriteLine($"Inactive Note Color: ({inactiveNoteColor.R},{inactiveNoteColor.G},{inactiveNoteColor.B})");
            Console.WriteLine($"Background Color: ({backgroundColor.R},{backgroundColor.G},{backgroundColor.B})");
            Console.WriteLine($"Guideline Color: ({guidelineColor.R},{guidelineColor.G},{guidelineColor.B})");
            Console.WriteLine("=============\n");

            List<Note> notes = new List<Note>();



            ParseMidiFile(filePath, notes);

            Console.WriteLine($"BPM: {bpm}");

            double totalDuration = 60 * notes[notes.Count - 1].End / (bpm * midiFile.DeltaTicksPerQuarterNote);



            Console.WriteLine($"Pixels Per Second: {pixelsPerSecond}");
            double pixelsPerFrame = pixelsPerSecond / fps;
            double pixelsPerBeat = pixelsPerSecond * 60 / bpm;
            double pixelsPerTick = pixelsPerBeat / midiFile.DeltaTicksPerQuarterNote;
            Console.WriteLine($"Pixels Per Frame: {pixelsPerFrame}");


            using var activeBrush = new SolidBrush(activeNoteColor);
            using var inactiveBrush = new SolidBrush(inactiveNoteColor);
            using var backgroundBrush = new SolidBrush(backgroundColor);
            using var guidelineBrush = new SolidBrush(guidelineColor);

            int offestX = 0;
            int totalFrames = (int)Math.Ceiling((totalDuration * fps));
            int noteScreenStartX = 0;
            int noteScreenEndX = 0;
            // Initialization stuff
            if (!notes.Any())
            {
                Console.WriteLine("Note list is empty");
                return;
            }
            //string midiFileDirectory = Path.GetDirectoryName(filePath); // Get MIDI file directory
            string appDirectory = AppContext.BaseDirectory; // Get .exe directory
            string midiFileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath); // Get filename without extension
            string frameDir = Path.Combine(appDirectory, midiFileNameWithoutExtension + "_frames");
            int counter = 0;
            string BaseFrameDir = frameDir;
            // Check if folder exists, append number if it does
            while (Directory.Exists(frameDir))
            {
                counter++;
                // Build new folder name, e.g. "MySong_frames_1"
                frameDir = $"{BaseFrameDir}_{counter}";
            }

            Directory.CreateDirectory(frameDir);
            int minPitch = notes.Min(note => note.Pitch);
            int maxPitch = notes.Max(note => note.Pitch);
            int mid = (minPitch + maxPitch) / 2;
            for (int i = 0; i < notes.Count; i++)
            {
                notes[i].PixelStartX = (long)(notes[i].Start * pixelsPerTick);
                notes[i].PixelLength = (long)((notes[i].End - notes[i].Start) * pixelsPerTick);
                notes[i].PixelEndX = (notes[i].PixelStartX + notes[i].PixelLength);
                notes[i].PixelY = canvasHeight / 2 + (mid - notes[i].Pitch) * noteHeight - noteHeight / 2; // Actually top-left
                notes[i].StartFrame = (int)(notes[i].Start * pixelsPerTick / pixelsPerFrame);
                notes[i].EndFrame = (int)(notes[i].End * pixelsPerTick / pixelsPerFrame);
                notes[i].UnidirectionalShake = UniformRandomExcludeMiddle(1, 0.7);
            }


            Console.CursorVisible = false;
            int lineStratX = LineEndX - guideLineWidth + 1;
            Action<Graphics, int, int> drawGuideline = (g, x, h) => { };
            int frameDigits = totalFrames.ToString().Length;
            string frameFormat = new string('0', Math.Max(2, frameDigits));
            Note? lastNote = notes.OrderByDescending(n => n.End).FirstOrDefault();
            int extraFrames = (int)Math.Ceiling(LineEndX / pixelsPerFrame);
            if (lastNote == null)
            {
                Console.WriteLine("Last note not found");
                return;
            }
            if (guideLineWidth > 0)
            {
                drawGuideline = (g, x, h) => g.FillRectangle(guidelineBrush, x, 0, guideLineWidth, h);
            }

            ConcurrentQueue<GeneratedFrame> framesQueue = new ConcurrentQueue<GeneratedFrame>();
            bool generationRunning = true;
            var saveFramesThread = new Thread(() =>
            {
                while (framesQueue.Count > 0 || generationRunning)
                {
                    if (!framesQueue.TryDequeue(out var generatedFrame))
                    {
                        Thread.Sleep(50);
                        continue;
                    }
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        string framePath = Path.Combine(frameDir, $"{generatedFrame.FrameIndex.ToString(frameFormat)}.png");
                        generatedFrame.Frame.Save(framePath, ImageFormat.Png);

                        generatedFrame.Frame.Dispose();
                    });
                }
            });
            saveFramesThread.IsBackground = true;
            saveFramesThread.Start();
            double shakePixels = (noteHeight * shakeAmplitude / 100.0);
            double shakeActivationPixels = (noteHeight * shakeActivation / 100.0);
            bool IsNoteActive = false;
            double timeToCenter = returnToCenterTime * fps;
            for (int frame = 0; frame <= (int)Math.Ceiling(lastNote.End * 60 * fps / (bpm * midiFile.DeltaTicksPerQuarterNote)) + extraFrames + 0.5 * fps; frame++)
            {
                offestX = (int)(pixelsPerFrame * frame);
                var bitmap = new Bitmap(canvasWidth, canvasHeight);
                using var graphics = Graphics.FromImage(bitmap);
                graphics.FillRectangle(backgroundBrush, 0, 0, canvasWidth, canvasHeight);


                while (framesQueue.Count >= 200)
                {
                    Thread.Sleep(50);
                }

                foreach (var note in notes)
                {
                    IsNoteActive = (note.PixelStartX - offestX + LineEndX <= LineEndX) && (LineEndX < note.PixelEndX - offestX + LineEndX);
                    noteScreenStartX = (int)(note.PixelStartX - offestX + LineEndX);
                    noteScreenEndX = (int)(note.PixelEndX - offestX + LineEndX);

                    if (noteScreenStartX > canvasWidth || noteScreenEndX < 0)
                    {
                        continue; // Skip if note is outside canvas range
                    }

                    System.Drawing.Drawing2D.GraphicsState originalState = graphics.Save();
                    graphics.TranslateTransform(noteScreenStartX, (note.PixelY + noteHeight / 2));
                    graphics.RotateTransform
                        (
                        (float)
                            (
                             !IsNoteActive ? 0 :
                            ShakeManner == 0 ?
                            EaseOutCubic((frame - note.StartFrame) / timeToCenter) * rotationAngle * UniformRandomExcludeMiddle(1, 0.7) * 50 * noteHeight / note.PixelLength :
                            EaseOutCubic((frame - note.StartFrame) / timeToCenter) * rotationAngle * UniformRandomExcludeMiddle(1, 0.7)
                            )
                        );

                    graphics.FillRectangle
                        (
                        IsNoteActive ? activeBrush : inactiveBrush,
                        //note.PixelStartX - offestX + LineEndX
                        0,

                        -noteHeight / 2 +
                        (mid - note.Pitch) * noteDistance +
                        (float)(IsNoteActive ? NormalRandom(shakeAmplitude, shakeAmplitudeVariance) : 0) +

                        (float)(!IsNoteActive ? 0 :
                        (
                        ((ShakeManner == 0) ?
                        EaseOutCubic((frame - note.StartFrame) / timeToCenter) * shakeActivationPixels * UniformRandomExcludeMiddle(1, 0.7) :
                        EaseOutCubic((frame - note.StartFrame) / timeToCenter) * shakeActivationPixels * note.UnidirectionalShake * shakeActivationPixels)
                        )),

                        note.PixelLength,
                        noteHeight
                        );
                    // Brush
                    // Note X
                    // Note Y
                    // Note Length
                    // Note Width (Height)

                    graphics.Restore(originalState); // Restore original state
                }
                drawGuideline(graphics, lineStratX, canvasHeight);
                framesQueue.Enqueue(new GeneratedFrame(frame, bitmap));
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($"Generating frame {frame + 1}/{totalFrames + 1 + extraFrames + 0.5 * fps}        "); // Spaces to overwrite previous output
            }
            generationRunning = false;
            saveFramesThread.Join();
            Console.CursorVisible = true;
            Console.WriteLine("\nAll frames generated. Starting video compilation...");

            // --- FFmpeg Video Compilation Logic ---
            string outputVideoPath = Path.Combine(appDirectory, $"{midiFileNameWithoutExtension}.mp4");
            
            // Handle duplicate filenames
            int vidCounter = 0;
            while (File.Exists(outputVideoPath))
            {
                vidCounter++;
                outputVideoPath = Path.Combine(appDirectory, $"{midiFileNameWithoutExtension}_{vidCounter}.mp4");
            }

            // Construct FFmpeg input pattern (e.g. %04d.png to match 0001.png)
            // Ensure we use forward slashes for the input string to avoid FFmpeg escaping issues, or quote properly.
            // Using absolute path for input pattern.
            string inputPattern = Path.Combine(frameDir, $"%0{Math.Max(2, frameDigits)}d.png");

            try
            {
                Console.WriteLine("Running FFmpeg...");
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    // Arguments explanation:
                    // -y: Overwrite output without asking
                    // -framerate {fps}: Set input framerate
                    // -i: Input file pattern
                    // -c:v libx264: Use H.264 codec for compatibility
                    // -pix_fmt yuv420p: Ensure pixel format is playable by standard players
                    // -vf: Video filters. 'pad' ensures width/height are divisible by 2 (required for YUV420P)
                    Arguments = $"-y -framerate {fps} -i \"{inputPattern}\" -c:v libx264 -pix_fmt yuv420p -vf \"pad=ceil(iw/2)*2:ceil(ih/2)*2\" \"{outputVideoPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var ffmpegProcess = new System.Diagnostics.Process { StartInfo = psi };
                
                ffmpegProcess.ErrorDataReceived += (s, e) => 
                {
                    if(!string.IsNullOrWhiteSpace(e.Data) && (e.Data.Contains("Error") || e.Data.Contains("frame=")))
                        Console.WriteLine($"[FFmpeg] {e.Data}"); 
                };

                ffmpegProcess.Start();
                ffmpegProcess.BeginErrorReadLine(); // FFmpeg logs to stderr
                ffmpegProcess.WaitForExit();

                if (ffmpegProcess.ExitCode == 0)
                {
                    Console.WriteLine($"\nVideo created successfully: {outputVideoPath}");
                    
                    Console.WriteLine("Deleting temporary image frames...");
                    try 
                    {
                        Directory.Delete(frameDir, true);
                        Console.WriteLine("Cleanup complete.");
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not delete temp folder. {ex.Message}");
                    }

                    // Open the compiled video
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                        {
                            FileName = outputVideoPath,
                            UseShellExecute = true
                        });
                    }
                    catch { /* Ignore if cannot open */ }
                }
                else
                {
                    Console.WriteLine($"\nFFmpeg failed with exit code {ffmpegProcess.ExitCode}.");
                    Console.WriteLine($"Frames have NOT been deleted. You can find them at: {frameDir}");
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                Console.WriteLine("\nERROR: FFmpeg not found!");
                Console.WriteLine("Please ensure 'ffmpeg' is installed and added to your system PATH.");
                Console.WriteLine($"Frames have been saved to: {frameDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nAn unexpected error occurred during video compilation: {ex.Message}");
                Console.WriteLine($"Frames have been saved to: {frameDir}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }




        // The following code is for outputting note info, currently commented out
        //foreach (var note in notes)
        //{
        //    // Output note info
        //    Console.WriteLine($"Note: {note.Name} ({note.Pitch}) Start: {note.Start} ticks End: {note.End} ticks Duration: {note.End - note.Start} ticks");
        //}
    }
    class Note
    {
        public long Start { get; }
        public long End { get; }
        public int Pitch { get; }
        public string Name { get; }
        public long PixelStartX { get; set; }
        public long PixelEndX { get; set; }
        public long PixelLength { get; set; }
        public int PixelY { get; set; }
        public int StartFrame { get; set; }
        public int EndFrame { get; set; }
        public double UnidirectionalShake { get; set; }
        public Note(long start, long end, int pitch, string name)
        {
            Start = start;
            End = end;
            Pitch = pitch;
            Name = name;
        }
    }
    public class GeneratedFrame
    {
        public int FrameIndex { get; }
        public Bitmap Frame { get; }
        public GeneratedFrame(int index, Bitmap frame)
        {
            FrameIndex = index;
            Frame = frame;
        }
    }
}