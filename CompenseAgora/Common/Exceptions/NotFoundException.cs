namespace CompenseAgora.Common.Exceptions;

public class NotFoundException(string entityName, object key)
    : Exception($"\"{entityName}\" com código {key} não foi encontrado(a).");
