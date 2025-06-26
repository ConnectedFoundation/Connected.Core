using Connected.Identities;
using Connected.Identities.Dtos;
using Connected.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Core.Identities.Users.Tests;

[TestClass]
public sealed class Users
{
	private long Id { get; set; }

	[AssemblyInitialize]
	public static void AssemblyInit(TestContext context)
	{
		new IdentitiesImage().Register();

		Application.RegisterMicroService(typeof(Storage.Sql.SqlStartup).Assembly);

		Task.Run(async () => { await Application.StartDefaultApplication([]); });

		while (!Application.HasStarted)
			Task.Delay(500);
	}

	[TestInitialize]
	public void TestInit()
	{
		// This method is called before each test method.
	}

	[TestMethod]
	[Priority(1)]
	public void Insert_User()
	{
		using var scope = Scope.Create();

		var users = scope.ServiceProvider.GetRequiredService<IUserService>();
		var dto = Dto.Factory.Create<IInsertUserDto>();

		dto.FirstName = "Tomaž";
		dto.LastName = "Pipinič";
		dto.Email = "tomaz.pipinic@tompit.com";
		dto.Password = "Hello";

		Id = users.Insert(dto).Result;

		Assert.IsTrue(Id > 0);
	}

	[TestMethod]
	[Priority(2)]
	public void Select_User_By_Id()
	{
		using var scope = Scope.Create();

		var users = scope.ServiceProvider.GetRequiredService<IUserService>();
		var dto = Dto.Factory.CreatePrimaryKey(Id);
		var user = users.Select(dto).Result;

		Assert.IsTrue(user is not null);
	}
}
