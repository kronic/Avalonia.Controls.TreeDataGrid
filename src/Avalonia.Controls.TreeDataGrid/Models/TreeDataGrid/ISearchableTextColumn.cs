namespace Avalonia.Controls.Models.TreeDataGrid
{
    public interface ISearchableTextColumn<TModel> : ITextSearchable
    {
        internal string? ResolveStingValueFromModel(TModel model);
    }
}
