namespace Connected.Services;
public interface IDependentPatchDto<THead, TPrimaryKey> : IDependentPrimaryKeyDto<THead, TPrimaryKey>, IPropertyProvider
	where THead : notnull
	where TPrimaryKey : notnull
{
}
