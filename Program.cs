using System;

namespace AllanBishop.XNA
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (LineOfSightDemo game = new LineOfSightDemo())
            {
                game.Run();
            }
        }
    }
}

