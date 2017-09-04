# Meme Machine 4
Meme machine 4 is a discord bot using .Net that is able to load in external plugins to extend functionality.

## Features
- Ability to load in external functionality 
- Initialization file for both Meme Machine 4 and plugins
- Ability to swap between release and debug mode.
- Built in Audio functionality from file or from stream.
- Other useful functions built in
- +Whatever features the plugins provide

Note, this project has no dedicated server. You will have to find a server to host it.
This project merely provides the equipment to run a discord bot, but not the hardware.

## Packages used and other dependencies
### [Discord.net](https://www.nuget.org/packages/Discord.Net)
### [Discord.Net.Providers.WS4Net](https://www.nuget.org/packages/Discord.Net.Providers.WS4Net/)
### Sodium
https://download.libsodium.org/libsodium/releases/
### Opus
Windows:
http://opus-codec.org/downloads/
(Note, you need to rename libopus-0.dll to opus.dll or it won't work)
Linux:
https://ftp.osuosl.org/pub/xiph/releases/opus/

Link to file contianing prebuilt binaries of Sodium and Opus for windows http://www.mediafire.com/file/9p0jaib6484zb3q/Opus%26Libsodium.7z

## How to make your own plugin
You must inherit the Plugin class from MemeMachine4.Plugin after it is built.
Here is a template to work from.
```csharp
public class YourFunction : Plugin
{
    //Important step. Pag is used for initialization of the Plugin class, but can also be intercepted.
    //This includes information about the .ini file.
    public YourFunction(PluginArgs pag) : base(pag)
    {
        Name = "Your plugin name";
        
        //This indicates what functions you will be using.
        //It uses the or operator to signify what functions it uses.
        //If you use one of these but don't override it, a NotImplementedException will be thrown.
        UsedFunctions = Functions.MessageReceived | Functions.MessageDeleted;
    }
    
    public async override Task MessageReceived(SocketMessage message)
    {
        //Your code here.
        //Note, this function is what you most likely will be using 99% of the time.
    }
    
    public async override Task MessageDeleted(Cacheable<IMessage, ulong> messageCach, ISocketMessageChannel message)
    {
        //Your code here.
    }
}
```
Build that plugin as a library and you are all set.

## Want some plugins?
Here are some plugins I've made for the project allready.
If anyone else makes plugins and has them on github, contact me! I'll gladly post it here.

[Mister 4 Eyes's plugins](https://github.com/Mister4Eyes/MM4-Plugins)

## Building from source
In order to build this project from source, there is a build order to it.
1. MemeMachine4.AudioHandler
2. MemeMachine4.PluginBase
3. MemeMachine4

Each subsiquent built needs a reference to the one before it. So Meme Machine needs a reference from both pluginBase and AudioHandler but PluginBase only needs a reference from Audio Handler.

Each of these are built from the csproj in windows.

Currently plans of getting this able to run on Linux are there but a few things are needed before that happens.

## Motivation
Meme Machine 4 was initially a personal bot for my server.
However after realizing that on other servers, instead of having one bot doing everything, there are multiple bots that only do one thing, multiple things, or even having overlapping functionallity. I decided to release my bot in hopes that it can reduce clutter in servers and possibly get more people to make bots for discord.
Meme machine 4 is designed with plugins in mind. For me, that was to keep things nice and organized having separate projects be their own thing.

## License
This is being released with Unlicense.
I can't be arsed to check if anyone is breaking a license I set up so this license most reflects how I feel.

![](https://upload.wikimedia.org/wikipedia/commons/6/62/PD-icon.svg)
