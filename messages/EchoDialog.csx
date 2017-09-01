using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Net;
using System.Text;
using System.Linq;
using System.Collections.Generic;

// For more information about this template visit http://aka.ms/azurebots-csharp-basic
[Serializable]
public class EchoDialog : IDialog<object>
{
    private string userName;
    protected int count = 1;
    private Dictionary<string, string> dicOfStrings = new Dictionary<string, string>();
    
    public EchoDialog(string name)
    {
        this.userName= name;
        
    }
    
    public Task StartAsync(IDialogContext context)
    {
        try
        {
            context.Wait(MessageReceivedAsync);
        }
        catch (OperationCanceledException error)
        {
            return Task.FromCanceled(error.CancellationToken);
        }
        catch (Exception error)
        {
            return Task.FromException(error);
        }

        return Task.CompletedTask;
    }

    public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
    {
        var message = await argument;
        if (message.Text == "reset")
        {
            PromptDialog.Confirm(
                context,
                AfterResetAsync,
                "Are you sure you want to reset the count?",
                "Didn't get that!",
                promptStyle: PromptStyle.Auto);
        }
        else if (message.Text.ToLower() == "hi")
        {
            await context.PostAsync($"Hi {userName}");
            context.Wait(MessageReceivedAsync);
        }
        else if (message.Text == "Bye")
        {
            await context.PostAsync($"Bye bye ");
            context.Wait(MessageReceivedAsync);
        }
        else if (message.Text.Contains("time"))
        {
            //Set the time zone information to US Mountain Standard Time 
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time"); 
            //Get date and time in US Mountain Standard Time 
            var dateTime = TimeZoneInfo.ConvertTime(DateTime.Now, timeZoneInfo);
            await context.PostAsync($"It´s {dateTime.ToString("HH:mm")} ");
            context.Wait(MessageReceivedAsync);
        }
         else if (message.Text == "food")
        {
            var food = ScrapeFood();
            await context.PostAsync(food);
            context.Wait(MessageReceivedAsync);
        }
        else
        {
            await context.PostAsync($"{this.count++}: You said {message.Text}");
            context.Wait(MessageReceivedAsync);
        }
    }

    public string ScrapeFood()
    {
        var date = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + (int)DayOfWeek.Monday);
        if (!dicOfStrings.Keys.Any(k => k.Contains(date.Day.ToString())))
        {
            //log.Info($"Fetching new data");
            WebClient client = new WebClient();
            client.Encoding = Encoding.UTF8;
            string downloadString = client.DownloadString("https://whiteshark.gastrogate.com/lunch/");
            
            while (downloadString.Contains("menu_header"))
            {
                
                var start = downloadString.IndexOf("menu_header") + 18;
                var matchDay = downloadString.Substring(start, 41);
                matchDay = matchDay.Replace("\t", "");
                matchDay = matchDay.Replace("\n", "");
                matchDay = matchDay.Replace("</td></tr>", "");
                matchDay = matchDay.Replace("<", "");

                var startFood = downloadString.IndexOf("td_title");
                var startPrice = downloadString.IndexOf("td_dbsk");
                var matchFood = downloadString.Substring(startFood, startPrice- startFood);
                matchFood = matchFood.Replace("td_title\"> <!-- td_title -->", "");
                matchFood = matchFood.Replace("\t", "");
                matchFood = matchFood.Replace("\n", "");
                matchFood = matchFood.Replace("</td></tr>", "");
                matchFood = matchFood.Replace("</td><td class=", "");
                matchFood = matchFood.Replace("\"", "");
                matchFood = matchFood.Replace("<br />", "");

                dicOfStrings.Add(matchDay, matchFood);
                downloadString = downloadString.Substring(startPrice+7);
            }
        }
                var resultString ="";
                foreach(var key in dicOfStrings.Keys)
                {
                   resultString= resultString + "\n" +($"{key} {dicOfStrings[key]}");
                }
        return resultString;
    }
    
    public async Task AfterResetAsync(IDialogContext context, IAwaitable<bool> argument)
    {
        var confirm = await argument;
        if (confirm)
        {
            this.count = 1;
            await context.PostAsync("Reset count.");
        }
        else
        {
            await context.PostAsync("Did not reset count.");
        }
        context.Wait(MessageReceivedAsync);
    }
}