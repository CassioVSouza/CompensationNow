using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.Dashboard.Queries.GetDashboard;

public record GetDashboardQuery(int CodigoPessoa) : IRequest<DashboardDto>;

public class GetDashboardQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentMonthStart = new DateOnly(today.Year, today.Month, 1);
        var windowStart = currentMonthStart.AddMonths(-11);
        var windowEndExclusive = currentMonthStart.AddMonths(1);

        var viagemRecords = await dbContext.Viagens
            .AsNoTracking()
            .Where(v => v.CodigoPessoa == request.CodigoPessoa
                        && v.DataReferencia >= windowStart
                        && v.DataReferencia < windowEndExclusive)
            .Select(v => new { v.DataReferencia, v.EmissaoCO2 })
            .ToListAsync(cancellationToken);

        var energiaRecords = await dbContext.Energias
            .AsNoTracking()
            .Where(e => e.CodigoPessoa == request.CodigoPessoa
                        && e.DataReferencia >= windowStart
                        && e.DataReferencia < windowEndExclusive)
            .Select(e => new { e.DataReferencia, e.EmissaoCO2 })
            .ToListAsync(cancellationToken);

        var compensacaoRecords = await dbContext.Compensacoes
            .AsNoTracking()
            .Where(c => c.CodigoPessoa == request.CodigoPessoa
                        && c.DataReferencia >= windowStart
                        && c.DataReferencia < windowEndExclusive)
            .Select(c => new { c.DataReferencia, c.QuantidadeCompensada })
            .ToListAsync(cancellationToken);

        var viagemByMonth = viagemRecords
            .GroupBy(v => (v.DataReferencia.Year, v.DataReferencia.Month))
            .ToDictionary(g => g.Key, g => g.Sum(v => v.EmissaoCO2));

        var energiaByMonth = energiaRecords
            .GroupBy(e => (e.DataReferencia.Year, e.DataReferencia.Month))
            .ToDictionary(g => g.Key, g => g.Sum(e => e.EmissaoCO2));

        var compensacaoByMonth = compensacaoRecords
            .GroupBy(c => (c.DataReferencia.Year, c.DataReferencia.Month))
            .ToDictionary(g => g.Key, g => g.Sum(c => c.QuantidadeCompensada));

        var monthlyEmissions = new List<MonthlyEmissionDto>();
        for (var monthStart = windowStart; monthStart <= currentMonthStart; monthStart = monthStart.AddMonths(1))
        {
            var key = (monthStart.Year, monthStart.Month);
            var viagemEmissao = viagemByMonth.GetValueOrDefault(key);
            var energiaEmissao = energiaByMonth.GetValueOrDefault(key);
            var compensacao = compensacaoByMonth.GetValueOrDefault(key);

            monthlyEmissions.Add(new MonthlyEmissionDto(monthStart.Year, monthStart.Month, viagemEmissao, energiaEmissao, compensacao));
        }

        var totalViagem = monthlyEmissions.Sum(m => m.ViagemEmissao);
        var totalEnergia = monthlyEmissions.Sum(m => m.EnergiaEmissao);
        var totalCompensacao = monthlyEmissions.Sum(m => m.Compensacao);

        return new DashboardDto(
            monthlyEmissions,
            totalViagem + totalEnergia,
            totalViagem,
            totalEnergia,
            totalCompensacao);
    }
}
