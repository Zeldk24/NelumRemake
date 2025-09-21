using System;
using Discord;
using UnityEngine;

public class DiscordManager : MonoBehaviour
{
    Discord.Discord discord;

    void Start()
    {
        discord = new Discord.Discord(1335772665939628064,(ulong)Discord.CreateFlags.NoRequireDiscord);
        ChangeActivity();

    }

    private void OnDisable()
    {
        discord.Dispose();
    }

    public void ChangeActivity()
    {
        var activityManager = discord.GetActivityManager();
        var activity = new Discord.Activity
        {
            State = "Jogando",
            Details = "A vida não é um morango",
            Assets =
            {
                LargeImage = "galocafe",
                LargeText = "",
                SmallText = "",
                SmallImage = "",
            },
            Timestamps =
            {
                Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            },
        };
        activityManager.UpdateActivity(activity, (res) =>
        {
            Debug.Log("Activity Updated");
        });
    }

    void Update()
    {
        discord.RunCallbacks();
    }
}