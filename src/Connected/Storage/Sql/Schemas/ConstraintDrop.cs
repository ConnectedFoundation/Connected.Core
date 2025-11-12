using System.Text;

namespace Connected.Storage.Sql.Schemas;

internal class ConstraintDrop(ObjectIndex index) : TableTransaction
{
	protected override async Task OnExecute()
	{
		await Context.Execute(CommandText);
	}
	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"ALTER TABLE {Escape(Context.Schema.Schema, Context.Schema.Name)} DROP CONSTRAINT {index.Name};");

			return text.ToString();
		}
	}
}
