using Connected.Identities.Dtos;
using Connected.Services;

namespace Connected.Identities.Users;

internal class InsertUser(IUserService users)
{
	public async Task<long> Invoke()
	{
		var dto = Dto.Factory.Create<IInsertUserDto>();

		dto.FirstName = "Tomaž";
		dto.LastName = "Pipinič";
		dto.Email = "tomaz.pipinic@tompit.com";
		dto.Password = "Hello";

		try
		{
			return await users.Insert(dto);
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);

			throw;
		}
	}
}
