using DX.Core.Types;

namespace DX.Infrastructure.Validation;

public interface ISqlQueryValidator
{
    DataSourceResult Validate(string query);
}
