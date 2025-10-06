using Connected.Core.Identities.Mock;
using Connected.Core.Identities.Mock.Dto;
using Connected.Core.Mock;
using Connected.Identities;
using Connected.Identities.Dtos;
using Connected.Services;

namespace Connected.Core.Identities.Test;

[TestClass]
public class Users()
	: RestTest(IdentitiesUrls.Users)
{
	private long Id { get; set; }
	private string? Password { get; set; }
	private string? Token { get; set; }
	private string? Email { get; set; }

	[TestInitialize]
	public async Task Initialize()
	{
		Password = ValueGenerator.Generate(32);

		var dto = new InsertUserDtoMock
		{
			FirstName = ValueGenerator.Generate(16),
			LastName = ValueGenerator.Generate(32),
			Email = $"{ValueGenerator.Generate(8)}@{ValueGenerator.Generate(4)}.com"
		};

		Email = dto.Email;

		Id = await Insert<InsertUserDtoMock, long>(dto);

		Assert.IsTrue(Id > 0);
	}

	[TestCleanup]
	public async Task Cleanup()
	{
		await Delete(Id);

		var entity = await Select<UserMock>(Id);

		Assert.IsNull(entity);
	}

	[TestMethod]
	public async Task Update()
	{
		var dto = new UpdateUserDtoMock
		{
			Id = Id,
			FirstName = ValueGenerator.Generate(16),
			LastName = ValueGenerator.Generate(32),
			Email = $"{ValueGenerator.Generate(8)}@{ValueGenerator.Generate(4)}.com",
			Status = UserStatus.Disabled
		};

		Email = dto.Email;

		await Update(dto);

		var entity = await Select<UserMock>(Id);

		Assert.IsNotNull(entity);
		Assert.AreEqual(entity.FirstName, dto.FirstName);
		Assert.AreEqual(entity.LastName, dto.LastName);
		Assert.AreEqual(entity.Email, dto.Email);
		Assert.AreEqual(entity.Status, dto.Status);
	}

	[TestMethod]
	public async Task Query()
	{
		var entities = await Query<UserMock>();

		Assert.IsNotNull(entities);
		Assert.IsTrue(entities.Count > 0);
	}

	[TestMethod]
	public async Task Lookup()
	{
		var entities = await Get<List<UserMock>>(IdentitiesUrls.LookupOperation,
		[
			new(nameof(IPrimaryKeyListDto<int>.Items), Id)
		]);

		Assert.IsNotNull(entities);
		Assert.IsTrue(entities.Count > 0);
	}

	[TestMethod]
	public async Task LookupByToken()
	{
		var entity = await Select<UserMock>(new Dictionary<string, object?>
		{
			{nameof(IUser.Id), Id }
		});

		Assert.IsNotNull(entity);

		var entities = await Get<List<UserMock>>(IdentitiesUrls.LookupByTokenOperation,
		[
			new (nameof(IPrimaryKeyListDto<int>.Items), entity.Token)
		]);

		Assert.IsNotNull(entities);
		Assert.IsTrue(entities.Count > 0);
	}

	[TestMethod]
	public async Task UpdatePassword()
	{
		var dto = new UpdateUserDtoMock
		{
			Id = Id,
			FirstName = ValueGenerator.Generate(16),
			LastName = ValueGenerator.Generate(32),
			Email = $"{ValueGenerator.Generate(8)}@{ValueGenerator.Generate(4)}.com",
			Status = UserStatus.Active
		};

		Email = dto.Email;

		await Update(dto);

		await Put(IdentitiesUrls.UpdatePasswordOperation, new UpdatePasswordDtoMock
		{
			Id = Id,
			Password = Password
		});

		var entity = await Put<UserMock>(IdentitiesUrls.SelectByCredentialsOperation, new Dictionary<string, object?>
		{
			{nameof(ISelectUserDto.User), Email },
			{nameof(ISelectUserDto.Password), Password }
		});

		Assert.IsNotNull(entity);
	}

	[TestMethod]
	public async Task Resolve()
	{
		var entity = await Get<UserMock>(IdentitiesUrls.ResolveOperation, new Dictionary<string, object?>
		{
			{nameof(IValueDto<int>.Value),  Email}
		});

		Assert.IsNotNull(entity);
	}
}
