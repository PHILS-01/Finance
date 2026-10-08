package com.lite.finance

object Analyzer {
    // key to label; all amounts in the same unit (e.g. Rs '000), shares in thousands
    val fields = listOf(
        "sales" to "Net sales", "cogs" to "Cost of sales", "admin" to "Admin expenses",
        "fin" to "Finance cost", "other" to "Other income/(expense), net", "tax" to "Tax",
        "dep" to "Depreciation + amortisation", "ca" to "Current assets", "cl" to "Current liabilities",
        "ta" to "Total assets", "eq" to "Equity", "debt" to "Borrowings (debt)", "cash" to "Cash",
        "rec" to "Trade receivables", "shares" to "Shares (thousands)", "cfo" to "Operating cash flow")

    class M(val name: String, val kind: Char, val f: (Map<String, Double>) -> Double)

    private fun Map<String, Double>.g(k: String) = this[k] ?: 0.0
    private fun d(a: Double, b: Double) = if (b == 0.0) Double.NaN else a / b
    private fun gp(m: Map<String, Double>) = m.g("sales") - m.g("cogs")
    private fun pbt(m: Map<String, Double>) = gp(m) - m.g("admin") - m.g("fin") + m.g("other")
    private fun np(m: Map<String, Double>) = pbt(m) - m.g("tax")
    private fun ebitda(m: Map<String, Double>) = pbt(m) + m.g("fin") + m.g("dep")
    private fun nd(m: Map<String, Double>) = m.g("debt") - m.g("cash")

    val metrics = listOf(
        M("Gross margin", '%') { d(gp(it), it.g("sales")) * 100 },
        M("Net margin", '%') { d(np(it), it.g("sales")) * 100 },
        M("Profit before tax", 'n') { pbt(it) },
        M("EBITDA", 'n') { ebitda(it) },
        M("EBITDA margin", '%') { d(ebitda(it), it.g("sales")) * 100 },
        M("Interest cover (EBIT/finance)", 'x') { d(pbt(it) + it.g("fin"), it.g("fin")) },
        M("Current ratio", 'x') { d(it.g("ca"), it.g("cl")) },
        M("Gearing (net debt/(net debt+equity))", '%') { d(nd(it), nd(it) + it.g("eq")) * 100 },
        M("Net debt / EBITDA", 'x') { d(nd(it), ebitda(it)) },
        M("Return on equity", '%') { d(np(it), it.g("eq")) * 100 },
        M("Return on assets", '%') { d(np(it), it.g("ta")) * 100 },
        M("EPS", 'e') { d(np(it), it.g("shares")) },
        M("Receivable days", 'n') { d(it.g("rec"), it.g("sales")) * 365 },
        M("Cash conversion (CFO/EBITDA)", '%') { d(it.g("cfo"), ebitda(it)) * 100 })

    fun fmt(v: Double, k: Char) = when {
        v.isNaN() || v.isInfinite() -> "n/a"
        k == '%' -> "%.1f%%".format(v); k == 'x' -> "%.2fx".format(v)
        k == 'e' -> "%.2f".format(v); else -> "%,.0f".format(v)
    }

    fun flags(m: Map<String, Double>): List<String> {
        val o = ArrayList<String>()
        fun v(n: String) = metrics.first { it.name.startsWith(n) }.f(m)
        if (v("Current ratio") < 1.1) o += "Liquidity thin: current ratio below 1.1x"
        if (v("Interest cover") < 2) o += "Interest cover below 2x"
        if (v("Gearing") > 65) o += "Gearing above 65%"
        if (v("Receivable days") > 60) o += "Receivables above 60 days of sales"
        if (v("Cash conversion") < 50) o += "Less than half of EBITDA converts to operating cash"
        if (v("Net debt") > 4) o += "Net debt above 4x EBITDA"
        return o
    }

    fun report(cur: Map<String, Double>, pri: Map<String, Double>): String {
        val sb = StringBuilder("Metric | Current | Prior | Change\n\n")
        for (x in metrics) {
            val a = x.f(cur); val b = x.f(pri)
            val ch = if (x.kind == '%' || x.kind == 'x') fmt(a - b, x.kind).let { if (a - b > 0) "+$it" else it }
                     else if (b == 0.0 || b.isNaN()) "n/a" else "%+.1f%%".format((a - b) / Math.abs(b) * 100)
            sb.append("${x.name}\n  ${fmt(a, x.kind)}  |  ${fmt(b, x.kind)}  |  $ch\n")
        }
        val fl = flags(cur)
        sb.append("\nFlags (current period)\n")
        sb.append(if (fl.isEmpty()) "None raised" else fl.joinToString("\n") { "- $it" })
        return sb.toString()
    }

    val sample: Pair<Map<String, Double>, Map<String, Double>> = Pair(
        mapOf("sales" to 21389781.0, "cogs" to 16922170.0, "admin" to 124341.0, "fin" to 2970526.0, "other" to -5955.0,
            "tax" to 11.0, "dep" to 804450.0, "ca" to 8198366.0, "cl" to 7905026.0, "ta" to 26808396.0, "eq" to 6644649.0,
            "debt" to 16300310.0, "cash" to 164083.0, "rec" to 7013598.0, "shares" to 474000.0, "cfo" to 1515078.0),
        mapOf("sales" to 11212576.0, "cogs" to 9056064.0, "admin" to 66189.0, "fin" to 1506534.0, "other" to -41312.0,
            "tax" to 0.0, "dep" to 413830.0, "ca" to 6839897.0, "cl" to 6587887.0, "ta" to 25755519.0, "eq" to 5277871.0,
            "debt" to 17368422.0, "cash" to 215974.0, "rec" to 5528786.0, "shares" to 474000.0, "cfo" to -4576277.0))
}
