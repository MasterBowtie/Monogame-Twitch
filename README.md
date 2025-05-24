# Monogame_Twitch Plugin #

## Installation ##

You will need to first build and initialize a monogame workspace into the desired folder with desired name.


```
$ dotnet new mgdesktopgl
```

You will need to add the TwitchLib to your package library
```
$ dotnet add package TwitchLib
```

From here you should clone this repository and paste into your new Monogame project, you will need to replace the `Content.mgcb` and `Game1.cs` files. You will need to change the namespace in `Game1.cs` to match your project namespace as is found in `Program.cs`

After this step has been done, you will need to make and fill in you `.env` file following the `.env.example` formula. To make the project be able to read your environment variables you will need to add the following lines to your `.csproj` file after the `</PropertyGroup>` section.
```
  <ItemGroup>
    <Content Include=".env">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
```

Monogame should be able to run and function with the ability to read chat from Twitch. To handle incoming messages from Twitch change the code in `Input/TwitchSocket.cs` on line 204 in the `onMessageReceived(object sender, OnMessageReceivedArgs e)` method.

Happy Gaming!!!

--MasterBowtie