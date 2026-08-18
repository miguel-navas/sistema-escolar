using FluentValidation;
using SistemaEscolar.Application.Alunos.Commands.CriarAluno;
using SistemaEscolar.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- Registro de dependências ---------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CriarAlunoCommand).Assembly));

builder.Services.AddValidatorsFromAssembly(typeof(CriarAlunoCommand).Assembly);

builder.Services.AdicionarInfrastructure(builder.Configuration);

// CORS liberado para o frontend web e apps mobile durante desenvolvimento.
// Restringir origens específicas antes de produção.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// --- Pipeline ---------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Exposto para os testes de integração (WebApplicationFactory).
public partial class Program { }
