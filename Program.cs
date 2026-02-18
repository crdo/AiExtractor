using Microsoft.Extensions.AI;

IChatClient client =
	new Azure.AI.OpenAI.AzureOpenAIClient(
			new Uri("https://hakenoai.openai.azure.com/"),
			new System.ClientModel.ApiKeyCredential("****API-KEY****"))
		.GetChatClient("gpt-4o")
		.AsIChatClient();

var response = await client.GetResponseAsync<Details>(
[
	new ChatMessage(ChatRole.User, [
		new TextContent("Extract information from the following image according to the JSON schema."),
		new DataContent(File.ReadAllBytes(@"D:\Temp\IMG_1463.JPG") , "image/jpeg") ,
	])
]);

Console.WriteLine(response.Result);

record Details
{
	public int NumberOfDogs { get; set; }
	public string FloorColor { get; set; }
}