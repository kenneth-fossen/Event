// See https://aka.ms/new-console-template for more information

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

Console.WriteLine("Hello, World!");
Console.WriteLine("CLI 4 event.bouvet.no");

// Your config
var eventId = "";
var token = "";

if (string.IsNullOrWhiteSpace(eventId))
{
    Console.WriteLine("EventID");
    eventId = Console.ReadLine();
}

if (string.IsNullOrWhiteSpace(token))
{
    Console.WriteLine("Token Please:");
    token = Console.ReadLine();
}


var httpClient = new HttpClient();

const string schema = "Bearer";
const string applicationJson = "application/json";
const string baseUrl = "https://event.bouvet.no";
const string endpoint = $"{baseUrl}/graphql";

var getParticipents = $$$"""
                         {
                           "operationName": "GetEventById",
                           "variables": {
                             "eventId": "{{{eventId}}}"
                           },
                           "query": "query GetEventById($eventId: String!) {\n  event(eventId: $eventId) {\n    id\n    participants {\n      name\n      comment\n      unit\n      acceptanceStatus\n      acceptanceStatusEnum\n      userId\n      photo\n      showAllergies\n      numberOfGuests\n      email\n      waitlistPosition\n      __typename\n    }\n    __typename\n  }\n}\n"
                         }
                         """;

var serializeOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};

httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(schema, token);
httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(applicationJson));
var response = await httpClient.PostAsync(endpoint, new StringContent(getParticipents, Encoding.UTF8, applicationJson));

Console.WriteLine($"HTTP Response Status: {response.StatusCode}");

if (response.IsSuccessStatusCode)
{
    Console.WriteLine("Wiii are GAME!");

    try
    {
        var content = await response.Content.ReadAsStringAsync();
        var entity = JsonSerializer.Deserialize<GrapQlReponse>(content, serializeOptions);
        Console.WriteLine();
        var count = entity.Data.Event.Participants.Count(p => p.AcceptanceStatus == "GOING");
        const string div = "-----------------------------------------------------------";
        Console.WriteLine(div);
        Console.WriteLine($"Attending: {entity.Data.Event.Id}: No: {count}");
        Console.WriteLine(div);
        Console.WriteLine($"| \t Name \t\t| Avdeling \t | Allergies Shared \t |");
        foreach (var participant in entity.Data.Event.Participants)
        {
            if (participant.AcceptanceStatus == "GOING")
            {
                var output = new StringBuilder();
                if (participant.Name.Length < 15 && !string.IsNullOrWhiteSpace(participant.Name))
                {
                    output.Append($"| {participant.Name} \t\t|");
                }
                else
                {
                    output.Append($"| {participant.Name} \t|");
                }

                output.Append($" {participant.Unit} | \t |{participant.ShowAllergies}");
                Console.WriteLine(output);
            }
        }
        Console.WriteLine(div);
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        Console.WriteLine("Cannot recover");
        Environment.Exit(0);
    }
}
else
{
    Console.WriteLine("Failed");
}


internal record GrapQlReponse(Data Data);

internal record Data(Event Event);

internal record Event(string Id, IList<Participant> Participants);

internal record Participant
{
    public string Name { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public string? Unit { get; set; }
    public string? AcceptanceStatus { get; set; }
    public string? UserId { get; set; }
    public string? Photo { get; set; }
    public bool? ShowAllergies { get; set; }
}