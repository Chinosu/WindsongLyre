using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus;
using DSharpPlus.Lavalink.Entities;
using DSharpPlus.Lavalink.EventArgs;
using System.Formats.Asn1;
using System.Xml.Linq;

namespace WindsongLyre
{
    public class SlashCommands : ApplicationCommandModule
    {
        public static List<Uri> TrackUriQueue = new();
        [SlashCommand("test", "A slash command made to test the DSharpPlus Slash Commands extension!")]
        public static async Task TestCommand(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Success~"));
        }

        [SlashCommand("delaytest", "A slash command made to test the DSharpPlus Slash Commands extension!")]
        public static async Task DelayTestCommand(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);

            // Some time consuming task like a database call or a complex operation

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Thanks for waiting!"));
        }

        [SlashCommand("join", "Join voice channel")]
        public static async Task JoinCommand(InteractionContext ctx)
        {
            var lava = ctx.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Lyre's connection is not established."));
                return;
            }
            var node = lava.ConnectedNodes.Values.First();
            var connection = node.GetGuildConnection(ctx.Guild);
            if (connection != null)
            {
                TrackUriQueue.Clear();
                await connection.DisconnectAsync();
            }
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You are not in a voice channel!"));
                return;
            }
            await node.ConnectAsync(ctx.Member.VoiceState.Channel);
            node.GetGuildConnection(ctx.Member.VoiceState.Guild).PlaybackFinished += async (connection, args) =>
            {
                if (connection.CurrentState.CurrentTrack == null)
                {
                    Console.WriteLine("next!");
                    if (TrackUriQueue.Count > 0)
                    {
                        await connection.PlayAsync((await node.Rest.GetTracksAsync(TrackUriQueue[0])).Tracks.First());
                        TrackUriQueue.RemoveAt(0);
                    }
                }
            };
            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Lyre joined {ctx.Member.VoiceState.Channel.Name}."));
        }
        [SlashCommand("leave", "Leave voice channel")]
        public static async Task LeaveCommand(InteractionContext ctx)
        {
            var lava = ctx.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Lyre'a connection is not established."));
                return;
            }
            var node = lava.ConnectedNodes.Values.First();
            var connection = node.GetGuildConnection(ctx.Guild);
            if (connection == null)
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Lyre is not connected to a voice channel."));
                return;
            }
            TrackUriQueue.Clear();
            await connection.DisconnectAsync();
            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Lyre left."));
        }
        [SlashCommand("play", "Play sound uwu")]
        public static async Task PlayCommand(InteractionContext ctx, [Option("search", "What are you looking for?")] string search)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You are not in a voice channel!"));
                return;
            }
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var connection = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            if (connection == null)
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Lyre is not in a voice channel."));
                return;
            }
            var loadResult = await node.Rest.GetTracksAsync(search);
            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(@"Search failed.¯\_(ツ)_/¯"));
                return;
            }
            var track = loadResult.Tracks.First();
            if (connection.CurrentState.CurrentTrack == null)
            {
                await connection.PlayAsync(track);
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"You searched for: {search}\nNow playing: {track.Uri}"));
            }
            else
            {
                TrackUriQueue.Add(track.Uri);
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"You searched for: {search}\nAdded to queue: {track.Uri}"));
            }
        }
        [SlashCommand("playurl", "Play sound with url instead")]
        public static async Task PlayUrlCommand(InteractionContext ctx, [Option("url", "What are you looking for?")] string url)
        {

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You are not in a voice channel!"));
                return;
            }
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var connection = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            if (connection == null)
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Lyre is not in a voice channel."));
                return;
            }
            var loadResult = await node.Rest.GetTracksAsync(new Uri(url, UriKind.Absolute));
            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(@"Search failed.¯\_(ツ)_/¯"));
                return;
            }
            var track = loadResult.Tracks.First();
            if (connection.CurrentState.CurrentTrack == null)
            {
                await connection.PlayAsync(track);
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Now playing: {track.Uri}"));
            }
            else
            {
                TrackUriQueue.Add(track.Uri);
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Added to queue: {track.Uri}"));
            }
        }
        [SlashCommand("pause", "Pause sound owo")]
        public static async Task PauseCommand(InteractionContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You are not in a voice channel!"));
                return;
            }
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var connection = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            if (connection == null)
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Lyre is not in a voice channel."));
                return;
            }
            if (connection.CurrentState.CurrentTrack == null)
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Lyre has nothing to pause."));
                return;
            }
            await connection.PauseAsync();
            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You're welcome."));
        }
        [SlashCommand("resume", "Resume sound uwu")]
        public static async Task ResumeCommand(InteractionContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You are not in a voice channel!"));
                return;
            }
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var connection = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            if (connection == null)
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Lyre is not in a voice channel."));
                return;
            }
            if (connection.CurrentState.CurrentTrack == null)
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Lyre has nothing to play."));
                return;
            }
            await connection.ResumeAsync();
            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You're welcome."));
        }
        [SlashCommand("stop", "Stop playing sound :c")]
        public static async Task StopCommand(InteractionContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You are not in a voice channel!"));
                return;
            }
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var connection = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            if (connection == null)
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Lyre is not in a voice channel."));
                return;
            }
            if (connection.CurrentState.CurrentTrack == null)
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Lyre has nothing to stop."));
                return;
            }
            TrackUriQueue.Clear();
            await connection.StopAsync();
            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Okay."));
        }
        [SlashCommand("skip", "Play the next sound uwu")]
        public static async Task SkipCommand(InteractionContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You are not in a voice channel!"));
                return;
            }
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var connection = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            if (connection == null)
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Lyre is not in a voice channel."));
                return;
            }
            if (TrackUriQueue.Count == 0)
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Lyre's queue is empty."));
                return;
            }
            await connection.PauseAsync();
            var track = (await node.Rest.GetTracksAsync(TrackUriQueue[0])).Tracks.First();
            TrackUriQueue.RemoveAt(0);
            await connection.PlayAsync(track);
            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Skipped.\nNow playing: {track.Uri}"));
        }

    }
}
