namespace Connected.Services;
public interface IDistributedPatchDto<THead, TPrimaryKey> : IDistributedPrimaryKeyDto<THead, TPrimaryKey>, IPropertyProvider
	where THead : notnull
	where TPrimaryKey : notnull
{
}
