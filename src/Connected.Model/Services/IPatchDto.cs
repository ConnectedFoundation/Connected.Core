namespace Connected.Services;
public interface IPatchDto<TPrimaryKey> : IPrimaryKeyDto<TPrimaryKey>, IPropertyProvider
		where TPrimaryKey : notnull
{

}
