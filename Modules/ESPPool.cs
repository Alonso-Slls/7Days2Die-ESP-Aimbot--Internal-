using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Game_7D2D.Modules
{
    /// <summary>
    /// Object pooling system for ESP rendering elements to reduce garbage collection overhead.
    /// Reuses commonly used rendering objects instead of creating/destroying them each frame.
    /// </summary>
    public static class ESPPool
    {
        // Pool for line rendering data
        private static Queue<LineData> linePool = new Queue<LineData>();
        private static Queue<BoxData> boxPool = new Queue<BoxData>();
        private static Queue<TextData> textPool = new Queue<TextData>();
        
        // Pool sizes to prevent excessive memory usage
        private const int MAX_POOL_SIZE = 1000;
        private const int INITIAL_POOL_SIZE = 100;
        
        // Performance tracking
        private static int totalLinesDrawn = 0;
        private static int totalBoxesDrawn = 0;
        private static int totalTextsDrawn = 0;
        
        static ESPPool()
        {
            // Pre-populate pools with initial objects
            for (int i = 0; i < INITIAL_POOL_SIZE; i++)
            {
                linePool.Enqueue(new LineData());
                boxPool.Enqueue(new BoxData());
                textPool.Enqueue(new TextData());
            }
        }
        
        /// <summary>
        /// Get a line data object from the pool or create a new one if pool is empty.
        /// </summary>
        public static LineData GetLine()
        {
            if (linePool.Count > 0)
            {
                return linePool.Dequeue();
            }
            return new LineData();
        }
        
        /// <summary>
        /// Return a line data object to the pool for reuse.
        /// </summary>
        public static void ReturnLine(LineData line)
        {
            if (linePool.Count < MAX_POOL_SIZE)
            {
                line.Reset();
                linePool.Enqueue(line);
            }
        }
        
        /// <summary>
        /// Get a box data object from the pool or create a new one if pool is empty.
        /// </summary>
        public static BoxData GetBox()
        {
            if (boxPool.Count > 0)
            {
                return boxPool.Dequeue();
            }
            return new BoxData();
        }
        
        /// <summary>
        /// Return a box data object to the pool for reuse.
        /// </summary>
        public static void ReturnBox(BoxData box)
        {
            if (boxPool.Count < MAX_POOL_SIZE)
            {
                box.Reset();
                boxPool.Enqueue(box);
            }
        }
        
        /// <summary>
        /// Get a text data object from the pool or create a new one if pool is empty.
        /// </summary>
        public static TextData GetText()
        {
            if (textPool.Count > 0)
            {
                return textPool.Dequeue();
            }
            return new TextData();
        }
        
        /// <summary>
        /// Return a text data object to the pool for reuse.
        /// </summary>
        public static void ReturnText(TextData text)
        {
            if (textPool.Count < MAX_POOL_SIZE)
            {
                text.Reset();
                textPool.Enqueue(text);
            }
        }
        
        /// <summary>
        /// Get performance statistics for the pooling system.
        /// </summary>
        public static string GetStats()
        {
            return $"Pool Stats - Lines: {linePool.Count}/{MAX_POOL_SIZE}, " +
                   $"Boxes: {boxPool.Count}/{MAX_POOL_SIZE}, " +
                   $"Texts: {textPool.Count}/{MAX_POOL_SIZE} | " +
                   $"Frame: {totalLinesDrawn}L, {totalBoxesDrawn}B, {totalTextsDrawn}T";
        }
        
        /// <summary>
        /// Reset frame counters for new frame.
        /// </summary>
        public static void ResetFrameCounters()
        {
            totalLinesDrawn = 0;
            totalBoxesDrawn = 0;
            totalTextsDrawn = 0;
        }
        
        /// <summary>
        /// Increment frame counters.
        /// </summary>
        public static void IncrementLineCount() => totalLinesDrawn++;
        public static void IncrementBoxCount() => totalBoxesDrawn++;
        public static void IncrementTextCount() => totalTextsDrawn++;
    }
    
    /// <summary>
    /// Data structure for line rendering.
    /// </summary>
    public class LineData
    {
        public Vector2 start;
        public Vector2 end;
        public Color color;
        public float thickness;
        
        public LineData() { Reset(); }
        
        public void Reset()
        {
            start = Vector2.zero;
            end = Vector2.zero;
            color = Color.white;
            thickness = 1f;
        }
        
        public void Set(Vector2 start, Vector2 end, Color color, float thickness)
        {
            this.start = start;
            this.end = end;
            this.color = color;
            this.thickness = thickness;
        }
    }
    
    /// <summary>
    /// Data structure for box rendering.
    /// </summary>
    public class BoxData
    {
        public float x, y, width, height;
        public Color color;
        public float thickness;
        public string text;
        
        public BoxData() { Reset(); }
        
        public void Reset()
        {
            x = y = width = height = 0f;
            color = Color.white;
            thickness = 1f;
            text = string.Empty;
        }
        
        public void Set(float x, float y, float width, float height, Color color, float thickness, string text = "")
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.color = color;
            this.thickness = thickness;
            this.text = text;
        }
    }
    
    /// <summary>
    /// Data structure for text rendering.
    /// </summary>
    public class TextData
    {
        public Vector2 position;
        public string text;
        public Color color;
        public int fontSize;
        
        public TextData() { Reset(); }
        
        public void Reset()
        {
            position = Vector2.zero;
            text = string.Empty;
            color = Color.white;
            fontSize = 12;
        }
        
        public void Set(Vector2 position, string text, Color color, int fontSize = 12)
        {
            this.position = position;
            this.text = text;
            this.color = color;
            this.fontSize = fontSize;
        }
    }
}
