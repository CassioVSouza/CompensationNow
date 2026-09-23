using System.Globalization;
using System.Reflection;
using Amazon.CognitoIdentityProvider;
using CompenseAgora.Auth;
using CompenseAgora.Common;
using CompenseAgora.Common.Behaviors;
using CompenseAgora.Components;
using CompenseAgora.Data;
using CompenseAgora.Data.Interceptors;
using CompenseAgora.Features.Energias.Calculo;
using CompenseAgora.Features.Viagens.Calculo;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

// This app has no multi-culture support (see CLAUDE.md: all user-facing text is pt-BR), so the
// culture is fixed process-wide rather than negotiated per-request. Without this, number/date
// parsing (MudNumericField, MudDatePicker, decimal.ToString/Parse) falls back to whatever culture
// the host OS happens to be running under, which on a non-pt-BR host would accept "." instead of
// "," as the decimal separator -- exactly backwards from what Brazilian users expect.
var culturaPadrao = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = culturaPadrao;
CultureInfo.DefaultThreadCurrentUICulture = culturaPadrao;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtual>();
builder.Services.AddScoped<AuditoriaInterceptor>();

builder.Services.AddDbContext<CompenseAgoraDbContext>((serviceProvider, options) =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CompenseAgoraDb"))
        .AddInterceptors(serviceProvider.GetRequiredService<AuditoriaInterceptor>()));

builder.Services.AddMudServices();

var applicationAssembly = Assembly.GetExecutingAssembly();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));
builder.Services.AddValidatorsFromAssembly(applicationAssembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddScoped<ICalculadoraEmissaoViagem, CalculadoraEmissaoViagem>();
builder.Services.AddScoped<ICalculadoraEmissaoEnergia, CalculadoraEmissaoEnergia>();

builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonCognitoIdentityProvider>();
builder.Services.Configure<CognitoOptions>(builder.Configuration.GetSection(CognitoOptions.SectionName));
builder.Services.AddScoped<CognitoAuthService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culturaPadrao),
    SupportedCultures = [culturaPadrao],
    SupportedUICultures = [culturaPadrao],
});

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
