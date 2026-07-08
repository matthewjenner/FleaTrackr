namespace FleaTrackr.Core.Pricing;

/// <summary>
/// Estimates the Escape from Tarkov flea-market sales fee, so profit can be shown net of it rather
/// than gross. Uses the community-documented BSG formula:
///
///   fee = VO*Ti*4^PO + VR*Tr*4^PR
///     VO = item base price, VR = listing (sale) price, Ti = Tr = 0.03
///     PO = log10(VO/VR), PR = log10(VR/VO)
///     PO is raised to ^1.08 when VR &lt; VO; PR is raised to ^1.08 when VR &gt;= VO
///
/// It is an estimate - real fees vary with quantity and hideout/skill bonuses - so callers should
/// label it as such. An optional reduction models the Intelligence Center / Hideout Management
/// discount as a flat percentage off the fee.
/// </summary>
public static class FleaFee
{
    private const double Ti = 0.03;
    private const double Tr = 0.03;

    /// <summary>
    /// Estimated fee in roubles to list one item of <paramref name="basePrice"/> at
    /// <paramref name="listPrice"/>. Returns 0 when either price is unknown/non-positive, so a
    /// missing base price simply yields no fee rather than a wrong one. <paramref name="reductionPercent"/>
    /// (0-100) discounts the fee (e.g. Intelligence Center).
    /// </summary>
    public static int Calculate(int basePrice, int listPrice, double reductionPercent = 0)
    {
        if (basePrice <= 0 || listPrice <= 0) return 0;

        double vo = basePrice;
        double vr = listPrice;
        double po = Math.Log10(vo / vr);
        double pr = Math.Log10(vr / vo);

        if (vr < vo) po = Math.Pow(po, 1.08);
        else pr = Math.Pow(pr, 1.08);

        double fee = vo * Ti * Math.Pow(4, po) + vr * Tr * Math.Pow(4, pr);
        fee *= 1 - Math.Clamp(reductionPercent, 0, 100) / 100.0;

        return fee > 0 ? (int)Math.Round(fee) : 0;
    }

    /// <summary>Net roubles received selling one item on the flea: list price minus the estimated fee.</summary>
    public static int NetSale(int basePrice, int listPrice, double reductionPercent = 0) =>
        listPrice - Calculate(basePrice, listPrice, reductionPercent);
}
