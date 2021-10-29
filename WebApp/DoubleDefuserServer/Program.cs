namespace DoubleDefuserServer
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            DoubleDefuserServer server = new DoubleDefuserServer();
            RT.PropellerApi.PropellerUtil.RunStandalone("./settings.txt", server);
        }
    }
}
