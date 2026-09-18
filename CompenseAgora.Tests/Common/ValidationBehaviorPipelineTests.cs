using System.Reflection;
using CompenseAgora.Common.Behaviors;
using CompenseAgora.Data;
using CompenseAgora.Features.Energias.Calculo;
using CompenseAgora.Features.Energias.Commands.CreateEnergia;
using CompenseAgora.Tests.Infrastructure;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CompenseAgora.Tests.Common;

/// <summary>
/// Wires a minimal ServiceCollection with AddMediatR + AddValidatorsFromAssembly + the ValidationBehavior
/// pipeline registration, the same way Program.cs does, to confirm an invalid command throws
/// FluentValidation.ValidationException before the handler body runs (rather than each handler validating
/// itself).
/// </summary>
public class ValidationBehaviorPipelineTests : IDisposable
{
    private readonly SqliteDbContextFactory _dbFactory = new();
    private readonly ServiceProvider _provider;

    public ValidationBehaviorPipelineTests()
    {
        var applicationAssembly = typeof(CompenseAgoraDbContext).Assembly;

        var services = new ServiceCollection();
        services.AddSingleton(_dbFactory.CreateContext());
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<ICalculadoraEmissaoEnergia, CalculadoraEmissaoEnergia>();

        _provider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _provider.Dispose();
        _dbFactory.Dispose();
    }

    [Fact]
    public async Task InvalidCommand_ThrowsValidationException_BeforeHandlerRuns()
    {
        var mediator = _provider.GetRequiredService<IMediator>();
        var dbContext = _provider.GetRequiredService<CompenseAgoraDbContext>();

        // Quantidade must be > 0 per CreateEnergiaCommandValidator; DataReferencia is also default here.
        var invalidCommand = new CreateEnergiaCommand(CodigoPessoa: 1, DataReferencia: default, Quantidade: -5);

        await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(invalidCommand));

        // The handler never ran, so nothing was persisted.
        Assert.Empty(dbContext.Energias);
    }

    [Fact]
    public async Task ValidCommand_PassesThroughToHandler()
    {
        var mediator = _provider.GetRequiredService<IMediator>();
        var dbContext = _provider.GetRequiredService<CompenseAgoraDbContext>();

        dbContext.Pessoas.Add(new CompenseAgora.Entities.Pessoa
        {
            Nome = "Ana",
            Sobrenome = "Silva",
            Email = "ana@example.com",
            CognitoSub = "sub-1",
        });
        await dbContext.SaveChangesAsync();
        var pessoaCodigo = dbContext.Pessoas.Single().Codigo;

        var validCommand = new CreateEnergiaCommand(
            CodigoPessoa: pessoaCodigo,
            DataReferencia: new DateOnly(2026, 1, 1),
            Quantidade: 10m);

        var codigo = await mediator.Send(validCommand);

        Assert.True(codigo > 0);
        Assert.Single(dbContext.Energias);
    }
}
