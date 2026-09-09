namespace TestZ.Common
{
    /// <summary>
    /// DTO payload's for protocol commands
    /// </summary>

    public sealed class HelloInfo
    {
       
        public string Domain { get; set; } = "";

       
        public string Machine { get; set; } = "";

     
        public string User { get; set; } = "";
    }

    public sealed class WelcomeInfo
    {
        public int HeartbeatIntervalSec { get; set; }
    }

    public sealed class HeartbeatInfo
    {
        public int IdleSeconds { get; set; }
    }

}
