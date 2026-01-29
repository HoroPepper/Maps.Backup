namespace Maps.Backup.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            bool isQuit = false;
            while (!isQuit)
            {
                QABackupWorkFlow workFlow = new QABackupWorkFlow();

                workFlow.Start("Customer_12345");

                if(Console.ReadLine() == "q")
                {
                    isQuit = true;
                }
            }

        }
    }
}
