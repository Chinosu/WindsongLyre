using DSharpPlus;
using DSharpPlus.Net;
using DSharpPlus.Lavalink;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace WindsongLyre;
internal class Program
{
    //public class tCommands : ApplicationCommandModule
    //{

    //}
    private static async Task Main(string[] args)
    {


        DiscordClient discord = new(new DiscordConfiguration
        {
            Token = "MTA5Mzc2MDc5OTg4Mjg5MTI4NA.GeYeiU.NPb1ZJiKhCKEM7aFOcenONtbZ0UlsEr9rbu1mc",
            TokenType = TokenType.Bot,
            //MinimumLogLevel = LogLevel.Debug
        });

        var slash = discord.UseSlashCommands();

        //const ulong testServerID = 1093775897510809680;
        //slash.RegisterCommands<tCommands>(testServerID);
        //slash.RegisterCommands<SlashCommands>(testServerID); //remove testServerID for non-testing launch
        slash.RegisterCommands<SlashCommands>();

        var endpoint = new ConnectionEndpoint
        {
            Hostname = "127.0.0.1",
            Port = 2333,
        };
        var lavalinkConfig = new LavalinkConfiguration
        {
            Password = "youshallnotpass",
            RestEndpoint = endpoint,
            SocketEndpoint = endpoint,
        };
        var lavalink = discord.UseLavalink();


        await discord.ConnectAsync();
        await lavalink.ConnectAsync(lavalinkConfig);


        await Task.Delay(-1);
    }
}