namespace ScreenSaver
{
    internal static class FrameLayout
    {
        public static void GetGridSize(int frameCount, out int columns, out int rows)
        {
            switch (frameCount)
            {
                case 4:
                    columns = 2;
                    rows = 2;
                    break;
                case 9:
                    columns = 3;
                    rows = 3;
                    break;
                case 13:
                    columns = 4;
                    rows = 4;
                    break;
                default:
                    columns = 1;
                    rows = 1;
                    break;
            }
        }

        public static void GetCellPosition(int frameIndex, int columns, out int column, out int row)
        {
            column = frameIndex % columns;
            row = frameIndex / columns;
        }
    }
}
