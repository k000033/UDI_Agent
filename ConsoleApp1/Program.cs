namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 设置为蓝色
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("{0,-18}", "這是藍色的文字");

            // 设置为绿色
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("{0,-18}", "這是綠色的文字");

            // 设置为洋红色
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("{0,-18}", "這是洋紅色的文字");

            // 重置颜色
            Console.ResetColor();

            // 添加换行
            Console.WriteLine();

        }
    }
}
