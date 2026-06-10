using Connected.Services;

namespace Connected.Entities;

public interface IEntityTopic<TEntity, TPrimaryKey>
	where TEntity : IEntity<TPrimaryKey>
	where TPrimaryKey : notnull
{
	Task<TPrimaryKey> Insert<TDto>(IServiceOperation<TDto> sender, params object[] propertySources)
		where TDto : IDto;
	Task Update<TDto>(IServiceOperation<TDto> sender, params object[] propertySources)
		where TDto : IPrimaryKeyDto<TPrimaryKey>;
	Task Patch<TUpdateDto>(IServiceOperation<IPatchDto<TPrimaryKey>> sender, params object[] propertySources)
		where TUpdateDto : IPrimaryKeyDto<TPrimaryKey>;
	/// <summary>
	/// This method is used when performing patch on one to one entities 
	/// and the caller doesn't know if the entity exists or not. 
	/// If the entity doesn't exist, it will be inserted and patch won't be performed.
	/// If the entity exists, patch will be performed.
	/// </summary>
	/// <remarks>
	/// Note that insert and update dtos must be compatible for this method to succeed.
	/// </remarks>
	/// <typeparam name="TUpdateDto"></typeparam>
	/// <typeparam name="TInsertDto"></typeparam>
	/// <param name="sender"></param>
	/// <returns></returns>
	Task Patch<TUpdateDto, TInsertDto>(IServiceOperation<IPatchDto<TPrimaryKey>> sender, params object[] propertySources)
		where TUpdateDto : IPrimaryKeyDto<TPrimaryKey>
		where TInsertDto : IDto;
	Task Delete(IServiceOperation<IPrimaryKeyDto<TPrimaryKey>> sender);
}
