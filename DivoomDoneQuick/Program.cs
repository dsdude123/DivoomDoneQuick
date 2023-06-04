using Diacritics.Extensions;
using HtmlAgilityPack;
using ImageMagick;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using DivoomDoneQuick;
using Newtonsoft.Json;

Config config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
string donationXpath = "/html/body/div[1]/h3";
string runsXpath = "/html/body/div[1]/table";
string eventXpath = "/html/body/div[1]/h2";

string eventName = "NO EVENT NAME FOUND";
string donations = "$0.00";

string scheduleOne = " ";
string scheduleTwo = " ";
string scheduleThree = " ";

var currentTime = DateTime.Now;

var donationTextSettings = new MagickReadSettings
{
    TextEncoding = Encoding.Unicode,
    Font = "PICO-8 mono.ttf",
    FontStyle = FontStyleType.Normal,
    FontPointsize = 4,
    FillColor = MagickColors.LimeGreen,
    BackgroundColor = MagickColors.Black,
    TextGravity = Gravity.Center,
    Width = 59,
    Height = 5
};

var eventTextSettings = new MagickReadSettings
{
    TextEncoding = Encoding.Unicode,
    Font = "PICO-8 mono.ttf",
    FontStyle = FontStyleType.Normal,
    FontPointsize = 4,
    FillColor = MagickColors.White,
    BackgroundColor = MagickColors.Black,
    TextGravity = Gravity.West,
    TextInterwordSpacing = 1,
    Width = 59,
    Height = 5
};

var scheduleTextSettings = new MagickReadSettings
{
    TextEncoding = Encoding.Unicode,
    Font = "PICO-8 mono.ttf",
    FontStyle = FontStyleType.Normal,
    FontPointsize = 4,
    FillColor = MagickColors.White,
    BackgroundColor = MagickColors.Black,
    TextGravity = Gravity.West,
    TextInterwordSpacing = 1,
    Width = 59,
    Height = 5
};


try
{
    var web = new HtmlWeb();
    var runsPage = web.Load(config.RunsUrl);
    var donationsPage = web.Load(config.DonationsUrl);


    var eventRawText = runsPage.DocumentNode.SelectSingleNode(eventXpath);
    if (eventRawText != null)
    {
        eventName = eventRawText.InnerText.Replace("Run Index\n&mdash;\n", "").Trim();
    }
    
    var donationsRawText = donationsPage.DocumentNode.SelectSingleNode(donationXpath);

    if (donationsRawText != null)
    {
        donations = donationsRawText.InnerText.Replace("\n\nTotal (Count):\n", "").Split(" ")[0];
    }
    
    var runsTable = runsPage.DocumentNode.SelectSingleNode(runsXpath);

    Run runOne = null;
    Run runTwo = null;
    Run runThree = null;

    if (runsTable != null)
    {
        var rows = runsTable.SelectNodes("tr");
        if (rows != null)
        foreach (var row in rows)
        {
            var cells = row.SelectNodes("td");
            Run nextRun = new Run();
            nextRun.Title = cells[0].InnerText.Replace("\n", "");
            nextRun.StartTime = DateTime.Parse(cells[5].InnerText);

            if (nextRun.StartTime <= currentTime) // We want to keep setting run one until we find runs in the future
            {
                runOne = nextRun;
            }
            else if (runOne == null) // The first row is in the future
            {
                runOne = nextRun;
            }
            else if (runTwo == null)
            {
                runTwo = nextRun;
            }
            else if (runThree == null)
            {
                runThree = nextRun;
            }
        }
    }

    bool liveNow = runOne != null && runOne.StartTime <= currentTime;
    bool futureSchedule = runOne != null && currentTime <= runOne.StartTime;
    bool eventNotSoon = runOne != null && (currentTime.AddHours(23) <= runOne.StartTime);
    bool eventOver = runOne != null && runTwo == null && runThree == null && currentTime >= runOne.StartTime.AddMinutes(30);
    bool liveNowOneRun = liveNow && runTwo == null;
    bool liveNowTwoRun = liveNow && runThree == null;
    bool futureScheduleOneRun = futureSchedule && runTwo == null;
    bool futureScheduleTwoRun = futureSchedule && runThree == null;

    if (runOne == null && runTwo == null && runThree == null) // Couldn't get any runs
    {
        scheduleOne = "SCHEDULE";
        scheduleTwo = "NOT";
        scheduleThree = "PUBLISHED";
        scheduleTextSettings.TextGravity = Gravity.Center;
    } else if (eventNotSoon) // Event doesn't start for 23 hours
    {
        var timespan = runOne.StartTime - currentTime;
        scheduleOne = "STARTS IN";
        scheduleTwo = $"{timespan.Days} DAYS";
        scheduleThree = $"{timespan.Hours} HOURS";
        scheduleTextSettings.TextGravity = Gravity.Center;
    } else if (eventOver) // Last schedule item was over 30 minutes ago (finale usually runs 5-20 minutes)
    {
        scheduleOne = "EVENT";
        scheduleTwo = "IS";
        scheduleThree = "OVER";
        scheduleTextSettings.TextGravity = Gravity.Center;
    } else if (futureScheduleOneRun)
    {
        scheduleOne = $"{runOne.StartTime.ToString("t")} - {runOne.Title}";
    } else if (futureScheduleTwoRun)
    {
        scheduleOne = $"{runOne.StartTime.ToString("t")} - {runOne.Title}";
        scheduleTwo = $"{runTwo.StartTime.ToString("t")} - {runTwo.Title}";
    } else if (futureSchedule)
    {
        scheduleOne = $"{runOne.StartTime.ToString("t")} - {runOne.Title}";
        scheduleTwo = $"{runTwo.StartTime.ToString("t")} - {runTwo.Title}";
        scheduleThree = $"{runThree.StartTime.ToString("t")} - {runThree.Title}";
    }
    else if (liveNowOneRun)
    {
        scheduleOne = $"NOW - {runOne.Title}";
    } else if (liveNowTwoRun)
    {
        scheduleOne = $"NOW - {runOne.Title}";
        scheduleTwo = $"{runTwo.StartTime.ToString("t")} - {runTwo.Title}";
    }
    else
    {
        scheduleOne = $"NOW - {runOne.Title}";
        scheduleTwo = $"{runTwo.StartTime.ToString("t")} - {runTwo.Title}";
        scheduleThree = $"{runThree.StartTime.ToString("t")} - {runThree.Title}";
    }
} catch (Exception e)
{
    Console.WriteLine("Failed to get data from GDQ tracker.");
}

eventName = WebUtility.HtmlDecode(eventName).RemoveDiacritics(); 
donations = WebUtility.HtmlDecode(donations).RemoveDiacritics();
scheduleOne = WebUtility.HtmlDecode(scheduleOne).RemoveDiacritics();
scheduleTwo = WebUtility.HtmlDecode(scheduleTwo).RemoveDiacritics();
scheduleThree = WebUtility.HtmlDecode(scheduleThree).RemoveDiacritics();

if (eventName.Length < 15) // 15 characters visible
{
    eventTextSettings.TextGravity = Gravity.Center;
}


var baseImage = new MagickImage("base.png");
var outputCollection = new MagickImageCollection();

/*
 * We wait for 20 frames and then scroll in the remaining 40.
 * If the text is longer than the number of frames, try to scroll at higher speed to
 * get all the text in. 
 */
var eventNameRemoveRate = eventName.Length > 40 ? ((int)Math.Ceiling(eventName.Length / 40.0)) : 1;
var scheduleOneRemoveRate = scheduleOne.Length > 40 ? ((int)Math.Ceiling(scheduleOne.Length / 40.0)) : 1;
var scheduleTwoRemoveRate = scheduleTwo.Length > 40 ? ((int)Math.Ceiling(scheduleTwo.Length / 40.0)) : 1;
var scheduleThreeRemoveRate = scheduleThree.Length > 40 ? ((int)Math.Ceiling(scheduleThree.Length / 40.0)) : 1;

for (int i = 0; i < 60; i++) // Maximum 60 frames
{
    MagickImage image = new MagickImage(baseImage);

    // Make sure the text we want to render fits within our character limit
    var safeEventText = eventName.Length <= 15 ? eventName : eventName.Substring(0, 16);
    var safeScheduleOneText = scheduleOne.Length <= 15 ? scheduleOne : scheduleOne.Substring(0, 16);
    var safeScheduleTwoText = scheduleTwo.Length <= 15 ? scheduleTwo : scheduleTwo.Substring(0, 16);
    var safeScheduleThreeText = scheduleThree.Length <= 15 ? scheduleThree : scheduleThree.Substring(0, 16);

    // Prevent ImageMagick escaping
    safeEventText = safeEventText.Replace("%", "\\%");
    safeScheduleOneText = safeScheduleOneText.Replace("%", "\\%");
    safeScheduleTwoText = safeScheduleTwoText.Replace("%", "\\%");
    safeScheduleThreeText = safeScheduleThreeText.Replace("%", "\\%");


    // Render and composite the text onto the base
    if (!string.IsNullOrWhiteSpace(safeEventText))
    {
        var eventTitleImage = new MagickImage($"label:{safeEventText}", eventTextSettings);
        image.Composite(eventTitleImage, 2, 22, CompositeOperator.SrcOver);
    }

    if (!string.IsNullOrWhiteSpace(donations))
    {
        var donationImage = new MagickImage($"label:{donations}", donationTextSettings);
        image.Composite(donationImage, 2, 30, CompositeOperator.SrcOver);
    }

    if (!string.IsNullOrWhiteSpace(safeScheduleOneText))
    {
        var scheduleOneImage = new MagickImage($"label:{safeScheduleOneText}", scheduleTextSettings);
        image.Composite(scheduleOneImage, 2, 37, CompositeOperator.SrcOver);
    }

    if (!string.IsNullOrWhiteSpace(safeScheduleTwoText))
    {
        var scheduleTwoImage = new MagickImage($"label:{safeScheduleTwoText}", scheduleTextSettings);
        image.Composite(scheduleTwoImage, 2, 45, CompositeOperator.SrcOver);
    }

    if (!string.IsNullOrWhiteSpace(safeScheduleThreeText))
    {
        var scheduleThreeImage = new MagickImage($"label:{safeScheduleThreeText}", scheduleTextSettings);
        image.Composite(scheduleThreeImage, 2, 53, CompositeOperator.SrcOver);
    }

    image.SetBitDepth(8);
    outputCollection.Add(new MagickImage(image));

    // If the text is longer than our limit, wait for a bit (20 frames) and then scroll it

    if (i > 19)
    {
        
        if (eventName.Length > 15)
        {
            eventName = eventName.Remove(0, eventNameRemoveRate);
        }

        if (scheduleOne.Length > 15)
        {
            scheduleOne = scheduleOne.Remove(0, scheduleOneRemoveRate);
        }

        if (scheduleTwo.Length > 15)
        {
            scheduleTwo = scheduleTwo.Remove(0, scheduleTwoRemoveRate);
        }

        if (scheduleThree.Length > 15)
        {
            scheduleThree = scheduleThree.Remove(0, scheduleThreeRemoveRate);
        }
    } 
}

#if DEBUG
await outputCollection.WriteAsync("gdq.gif");
#endif

MemoryStream memoryStream = new MemoryStream();
await outputCollection.WriteAsync(memoryStream, MagickFormat.Gif);

memoryStream.Position = 0;
byte[] byteData = new byte[memoryStream.Length];
await memoryStream.ReadAsync(byteData, 0, byteData.Length);

var httpClient = new HttpClient();
var byteArrayContent = new ByteArrayContent(byteData);
httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
byteArrayContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/gif");

var rsp = await httpClient.PostAsync("http://localhost:5000/sendGif", new MultipartFormDataContent
    {
        {byteArrayContent, "\"gif\"", "\"gdq.gif\""},
        {new StringContent("200"), "\"speed\""},
        {new StringContent("false"), "\"skip_first_frame\""}
    });

Environment.Exit(0);