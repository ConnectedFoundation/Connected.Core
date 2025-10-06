using Connected.Core.Identities.Mock;
using Connected.Core.Identities.Mock.Dto;
using Connected.Core.Mock;
using Connected.Core.Services.Mock;
using Connected.Identities;

namespace Connected.Core.Identities.Test;

[TestClass]
public class Users()
	: RestTest(IdentitiesUrls.Users)
{
	[TestMethod]
	public async Task Crud()
	{
		var dto = new InsertUserDtoMock
		{
			FirstName = ValueGenerator.Generate(16),
			LastName = ValueGenerator.Generate(32),
			Email = $"{ValueGenerator.Generate(8)}@{ValueGenerator.Generate(4)}.com",
			Password = ValueGenerator.Generate(10)
		};

		var id = await Insert<InsertUserDtoMock, long>(dto);

		Assert.IsTrue(id > 0);

		var updateDto = new UpdateUserDtoMock
		{
			Id = id,
			FirstName = ValueGenerator.Generate(16),
			LastName = ValueGenerator.Generate(32),
			Email = $"{ValueGenerator.Generate(8)}@{ValueGenerator.Generate(4)}.com",
			Status = UserStatus.Disabled
		};

		await Update(updateDto);

		var entity = await Select<PrimaryKeyDtoMock<long>, UserMock>(id);

		Assert.IsNotNull(entity);
		Assert.AreEqual(entity.FirstName, updateDto.FirstName);
		Assert.AreEqual(entity.LastName, updateDto.LastName);
		Assert.AreEqual(entity.Email, updateDto.Email);
		Assert.AreEqual(entity.Status, updateDto.Status);

		await Delete<PrimaryKeyDtoMock<long>>(id);
	}

	[TestMethod]
	public async Task QueryLookup()
	{
		var dto = new InsertUserDtoMock
		{
			FirstName = ValueGenerator.Generate(16),
			LastName = ValueGenerator.Generate(32),
			Email = $"{ValueGenerator.Generate(8)}@{ValueGenerator.Generate(4)}.com"
		};

		var id = await Insert<InsertUserDtoMock, long>(dto);
		var entities = await Query<DtoMock, UserMock>(null);

		Assert.IsNotNull(entities);
		Assert.IsTrue(entities.Count > 0);

		entities = await GetList<PrimaryKeyListDtoMock<long>, UserMock>(IdentitiesUrls.LookupOperation, new(id));

		Assert.IsNotNull(entities);
		Assert.IsTrue(entities.Count > 0);

		await Delete<PrimaryKeyDtoMock<long>>(id);
	}

	[TestMethod]
	public async Task LookupByToken()
	{
		var dto = new InsertUserDtoMock
		{
			FirstName = ValueGenerator.Generate(16),
			LastName = ValueGenerator.Generate(32),
			Email = $"{ValueGenerator.Generate(8)}@{ValueGenerator.Generate(4)}.com"
		};

		var id = await Insert<InsertUserDtoMock, long>(dto);

		Assert.IsTrue(id > 0);

		var entity = await Select<PrimaryKeyDtoMock<long>, UserMock>(id);

		Assert.IsNotNull(entity);

		var entities = await GetList<ValueListDtoMock<string>, UserMock>(IdentitiesUrls.LookupByTokenOperation, new(entity.Token));

		Assert.IsNotNull(entities);
		Assert.IsTrue(entities.Count > 0);

		await Delete<PrimaryKeyDtoMock<long>>(id);
	}

	[TestMethod]
	public async Task UpdatePassword()
	{
		var dto = new InsertUserDtoMock
		{
			FirstName = ValueGenerator.Generate(16),
			LastName = ValueGenerator.Generate(32),
			Email = $"{ValueGenerator.Generate(8)}@{ValueGenerator.Generate(4)}.com"
		};

		var id = await Insert<InsertUserDtoMock, long>(dto);

		Assert.IsTrue(id > 0);

		var email = $"{ValueGenerator.Generate(8)}@{ValueGenerator.Generate(4)}.com";

		var updateDto = new UpdateUserDtoMock
		{
			Id = id,
			FirstName = ValueGenerator.Generate(16),
			LastName = ValueGenerator.Generate(32),
			Email = email,
			Status = UserStatus.Active
		};

		await Update(updateDto);

		var password = ValueGenerator.Generate(10);

		await Put(IdentitiesUrls.UpdatePasswordOperation, new UpdatePasswordDtoMock
		{
			Id = id,
			Password = password
		});

		var entity = await Put<SelectUserDtoMock, UserMock>(IdentitiesUrls.SelectByCredentialsOperation, new()
		{
			User = email,
			Password = password
		});

		Assert.IsNotNull(entity);

		await Delete<PrimaryKeyDtoMock<long>>(id);
	}

	[TestMethod]
	public async Task Resolve()
	{
		var dto = new InsertUserDtoMock
		{
			FirstName = ValueGenerator.Generate(16),
			LastName = ValueGenerator.Generate(32),
			Email = $"{ValueGenerator.Generate(8)}@{ValueGenerator.Generate(4)}.com"
		};

		var id = await Insert<InsertUserDtoMock, long>(dto);

		Assert.IsTrue(id > 0);

		var entity = await Select<PrimaryKeyDtoMock<long>, UserMock>(id);

		Assert.IsNotNull(entity);

		entity = await Get<ValueDtoMock<string>, UserMock>(IdentitiesUrls.ResolveOperation, entity.Email!);

		Assert.IsNotNull(entity);

		entity = await Get<ValueDtoMock<string>, UserMock>(IdentitiesUrls.ResolveOperation, entity.Token);

		Assert.IsNotNull(entity);

		entity = await Get<ValueDtoMock<string>, UserMock>(IdentitiesUrls.ResolveOperation, entity.Id.ToString());

		Assert.IsNotNull(entity);
	}
}
