// See https://aka.ms/new-console-template for more information

using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

Console.WriteLine("Hello, World!");
Console.WriteLine("CLI 4 event.bouvet.no");

// Your config

var token = "";


if (string.IsNullOrWhiteSpace(token))
{
    Console.WriteLine("Token Please:");
    token = Console.ReadLine();
}


var httpClient = new HttpClient();
const string div = "-----------------------------------------------------------";
const string schema = "Bearer";
const string applicationJson = "application/json";
const string baseUrl = "https://event.bouvet.no";
const string endpoint = $"{baseUrl}/graphql";

var getEvents = $$$"""
                {
                  "operationName": "GetInvitedEvents",
                  "variables": {},
                  "query": "query GetInvitedEvents {\n  invitedEvents {\n    id\n    eventName\n    description\n    location\n    summary\n    startDate\n    endDate\n    responseDeadline\n    minParticipants\n    maxParticipants\n    numberOfGuestsAllowed\n    requireResponse\n    attendenceCount\n    isSocial\n    ownerName\n    collaborators\n    __typename\n  }\n}\n"
                }
""";

var serializeOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};

httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(schema, token);
httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(applicationJson));


var eventResponse = await httpClient.PostAsync(endpoint, new StringContent(getEvents, Encoding.UTF8, applicationJson));
eventResponse.EnsureSuccessStatusCode();
var eventListResponseContent = await eventResponse.Content.ReadAsStringAsync();
var eventListSelector = new Dictionary<int, string>();
try
{
    var eventList = JsonSerializer.Deserialize<GrapQlReponse>(eventListResponseContent, serializeOptions);

    Console.WriteLine(div);
    foreach (var (invitedEvent, idx) in eventList.Data.InvitedEvents.Select((events, i) => (events, i)))
    {
        Console.WriteLine($"{idx} \t | {invitedEvent.Id} \t | {invitedEvent.EventName}");
        eventListSelector.Add(idx, invitedEvent.Id);
    }
    Console.WriteLine(div);
}
catch (Exception e)
{
    Console.WriteLine(e);
    throw;
}


Console.WriteLine("Select the ID (int) for the Event:");
var selectedEventInt = Console.ReadLine();

eventListSelector.TryGetValue(int.Parse(selectedEventInt), out var selectedEvent );

var getParticipents = $$$"""
                         {
                           "operationName": "GetEventById",
                           "variables": {
                             "eventId": "{{{selectedEvent}}}"
                           },
                           "query": "query GetEventById($eventId: String!) {\n  event(eventId: $eventId) {\n    id\n    participants {\n      name\n      comment\n      unit\n      acceptanceStatus\n      acceptanceStatusEnum\n      userId\n      photo\n      showAllergies\n      numberOfGuests\n      email\n      waitlistPosition\n      __typename\n    }\n    __typename\n  }\n}\n"
                         }
                         """;


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

internal class Data {
    public IList<InvitedEvents> InvitedEvents { get; set; }
    public Event Event { get; set; }
}


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

internal class InvitedEvents
{
    public string Id { get; set; }
    public string EventName { get; set; }
    public string Owner { get; set; }
    public string StartDate { get; set; }
    public string EndDate { get; set; }
}