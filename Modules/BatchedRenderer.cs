using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game_7D2D.Modules
{
    /// <summary>
    /// Batched rendering system for optimized OnGUI performance.
    /// Groups draw calls, minimizes state changes, and precomputes positions.
    /// </summary>
    public static class BatchedRenderer
    {
        private static bool _initialized = false;
        
        // Render batches for different primitive types
        private static List<LineBatch> _lineBatches = new List<LineBatch>();
        private static List<BoxBatch> _boxBatches = new List<BoxBatch>();
        private static List<TextBatch> _textBatches = new List<TextBatch>();
        private static List<CircleBatch> _circleBatches = new List<CircleBatch>();
        
        // Performance tracking
        private static int _totalDrawCalls = 0;
        private static int _batchedDrawCalls = 0;
        private static float _lastRenderTime = 0f;
        
        // Color state tracking to minimize changes
        private static Color _lastColor = Color.white;
        private static float _lastThickness = 1f;
        
        /// <summary>
        /// Initialize the batched rendering system.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            
            _lineBatches = new List<LineBatch>();
            _boxBatches = new List<BoxBatch>();
            _textBatches = new List<TextBatch>();
            _circleBatches = new List<CircleBatch>();
            
            _initialized = true;
            ErrorHandler.LogInfo("BatchedRenderer", "Batched rendering system initialized");
        }
        
        /// <summary>
        /// Clear all render batches for the current frame.
        /// </summary>
        public static void ClearBatches()
        {
            _lineBatches.Clear();
            _boxBatches.Clear();
            _textBatches.Clear();
            _circleBatches.Clear();
            _totalDrawCalls = 0;
            _batchedDrawCalls = 0;
        }
        
        /// <summary>
        /// Add a line to the render batch.
        /// </summary>
        public static void AddLine(Vector2 start, Vector2 end, Color color, float thickness = 1f)
        {
            _lineBatches.Add(new LineBatch
            {
                start = start,
                end = end,
                color = color,
                thickness = thickness
            });
            _totalDrawCalls++;
        }
        
        /// <summary>
        /// Add a box to the render batch.
        /// </summary>
        public static void AddBox(float x, float y, float width, float height, Color color, float thickness = 2f)
        {
            _boxBatches.Add(new BoxBatch
            {
                x = x,
                y = y,
                width = width,
                height = height,
                color = color,
                thickness = thickness
            });
            _totalDrawCalls++;
        }
        
        /// <summary>
        /// Add text to the render batch.
        /// </summary>
        public static void AddText(Vector2 position, string text, Color color, int fontSize = 12)
        {
            _textBatches.Add(new TextBatch
            {
                position = position,
                text = text,
                color = color,
                fontSize = fontSize
            });
            _totalDrawCalls++;
        }
        
        /// <summary>
        /// Add a circle to the render batch.
        /// </summary>
        public static void AddCircle(Vector2 center, float radius, Color color, int segments = 32)
        {
            _circleBatches.Add(new CircleBatch
            {
                center = center,
                radius = radius,
                color = color,
                segments = segments
            });
            _totalDrawCalls++;
        }
        
        /// <summary>
        /// Execute all batched render operations.
        /// </summary>
        public static void RenderBatches()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                // Render lines grouped by color and thickness
                RenderLineBatches();
                
                // Render boxes grouped by color and thickness
                RenderBoxBatches();
                
                // Render circles grouped by color
                RenderCircleBatches();
                
                // Render text (can't batch as easily due to different fonts/sizes)
                RenderTextBatches();
                
                stopwatch.Stop();
                _lastRenderTime = stopwatch.ElapsedMilliseconds;
                
                ErrorHandler.LogDebug("BatchedRenderer", 
                    $"Rendered {_totalDrawCalls} draw calls in {_batchedDrawCalls} batches ({_lastRenderTime}ms)");
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("BatchedRenderer", $"Batched rendering failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Render line batches grouped by color and thickness.
        /// </summary>
        private static void RenderLineBatches()
        {
            if (_lineBatches.Count == 0) return;
            
            // Group lines by color and thickness
            var groupedLines = new Dictionary<(Color color, float thickness), List<LineBatch>>();
            
            foreach (var line in _lineBatches)
            {
                var key = (line.color, line.thickness);
                if (!groupedLines.ContainsKey(key))
                {
                    groupedLines[key] = new List<LineBatch>();
                }
                groupedLines[key].Add(line);
            }
            
            // Render each group
            foreach (var group in groupedLines)
            {
                SetRenderState(group.Key.color, group.Key.thickness);
                
                foreach (var line in group.Value)
                {
                    Render.DrawLine(line.start, line.end, line.color, line.thickness);
                }
                
                _batchedDrawCalls++;
            }
        }
        
        /// <summary>
        /// Render box batches grouped by color and thickness.
        /// </summary>
        private static void RenderBoxBatches()
        {
            if (_boxBatches.Count == 0) return;
            
            // Group boxes by color and thickness
            var groupedBoxes = new Dictionary<(Color color, float thickness), List<BoxBatch>>();
            
            foreach (var box in _boxBatches)
            {
                var key = (box.color, box.thickness);
                if (!groupedBoxes.ContainsKey(key))
                {
                    groupedBoxes[key] = new List<BoxBatch>();
                }
                groupedBoxes[key].Add(box);
            }
            
            // Render each group
            foreach (var group in groupedBoxes)
            {
                SetRenderState(group.Key.color, group.Key.thickness);
                
                foreach (var box in group.Value)
                {
                    Render.DrawBox(box.x, box.y, box.width, box.height, box.color, box.thickness);
                }
                
                _batchedDrawCalls++;
            }
        }
        
        /// <summary>
        /// Render circle batches grouped by color.
        /// </summary>
        private static void RenderCircleBatches()
        {
            if (_circleBatches.Count == 0) return;
            
            // Group circles by color
            var groupedCircles = new Dictionary<Color, List<CircleBatch>>();
            
            foreach (var circle in _circleBatches)
            {
                if (!groupedCircles.ContainsKey(circle.color))
                {
                    groupedCircles[circle.color] = new List<CircleBatch>();
                }
                groupedCircles[circle.color].Add(circle);
            }
            
            // Render each group
            foreach (var group in groupedCircles)
            {
                SetRenderState(group.Key, 1f);
                
                foreach (var circle in group.Value)
                {
                    Render.DrawCircle(circle.center, (int)circle.radius, circle.color, 1f, false, circle.segments);
                }
                
                _batchedDrawCalls++;
            }
        }
        
        /// <summary>
        /// Render text batches (minimal batching possible).
        /// </summary>
        private static void RenderTextBatches()
        {
            if (_textBatches.Count == 0) return;
            
            // Group text by color and font size
            var groupedText = new Dictionary<(Color color, int fontSize), List<TextBatch>>();
            
            foreach (var text in _textBatches)
            {
                var key = (text.color, text.fontSize);
                if (!groupedText.ContainsKey(key))
                {
                    groupedText[key] = new List<TextBatch>();
                }
                groupedText[key].Add(text);
            }
            
            // Render each group
            foreach (var group in groupedText)
            {
                SetRenderState(group.Key.color, 1f);
                
                foreach (var text in group.Value)
                {
                    Render.DrawString(text.position, text.text);
                }
                
                _batchedDrawCalls++;
            }
        }
        
        /// <summary>
        /// Set render state only when it changes to minimize state changes.
        /// </summary>
        private static void SetRenderState(Color color, float thickness)
        {
            if (color != _lastColor)
            {
                GUI.color = color;
                _lastColor = color;
            }
            
            if (Math.Abs(thickness - _lastThickness) > Config.THICKNESS_TOLERANCE)
            {
                _lastThickness = thickness;
            }
        }
        
        /// <summary>
        /// Get performance statistics.
        /// </summary>
        public static string GetPerformanceStats()
        {
            return $"Draw Calls: {_totalDrawCalls}, Batches: {_batchedDrawCalls}, Time: {_lastRenderTime}ms";
        }
        
        /// <summary>
        /// Batch data structures.
        /// </summary>
        private struct LineBatch
        {
            public Vector2 start;
            public Vector2 end;
            public Color color;
            public float thickness;
        }
        
        private struct BoxBatch
        {
            public float x, y, width, height;
            public Color color;
            public float thickness;
        }
        
        private struct TextBatch
        {
            public Vector2 position;
            public string text;
            public Color color;
            public int fontSize;
        }
        
        private struct CircleBatch
        {
            public Vector2 center;
            public float radius;
            public Color color;
            public int segments;
        }
    }
}
