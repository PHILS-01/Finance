package com.lite.finance

import android.app.Activity
import android.os.Bundle
import android.text.InputType
import android.view.Gravity
import android.view.ViewGroup.LayoutParams.MATCH_PARENT
import android.view.ViewGroup.LayoutParams.WRAP_CONTENT
import android.widget.*

class MainActivity : Activity() {
    private val cur = HashMap<String, EditText>()
    private val pri = HashMap<String, EditText>()
    private lateinit var out: TextView

    override fun onCreate(b: Bundle?) {
        super.onCreate(b)
        val prefs = getSharedPreferences("fin", MODE_PRIVATE)
        val root = LinearLayout(this).apply { orientation = LinearLayout.VERTICAL; setPadding(24, 24, 24, 24) }
        root.addView(TextView(this).apply { text = "FinLite: financial statement analyser"; textSize = 20f })
        root.addView(TextView(this).apply { text = "Enter amounts in one unit (e.g. Rs '000). Prior period is optional." })
        root.addView(LinearLayout(this).apply {
            addView(TextView(context).apply { text = "Item"; layoutParams = LinearLayout.LayoutParams(0, WRAP_CONTENT, 1.4f) })
            addView(TextView(context).apply { text = "Current"; layoutParams = LinearLayout.LayoutParams(0, WRAP_CONTENT, 1f) })
            addView(TextView(context).apply { text = "Prior"; layoutParams = LinearLayout.LayoutParams(0, WRAP_CONTENT, 1f) })
        })
        for ((k, label) in Analyzer.fields) {
            val r = LinearLayout(this).apply { gravity = Gravity.CENTER_VERTICAL }
            r.addView(TextView(this).apply { text = label; textSize = 13f; layoutParams = LinearLayout.LayoutParams(0, WRAP_CONTENT, 1.4f) })
            for ((map, p) in listOf(cur to "c_", pri to "p_")) {
                val e = EditText(this).apply {
                    inputType = InputType.TYPE_CLASS_NUMBER or InputType.TYPE_NUMBER_FLAG_DECIMAL or InputType.TYPE_NUMBER_FLAG_SIGNED
                    textSize = 13f; setText(prefs.getString(p + k, "")); layoutParams = LinearLayout.LayoutParams(0, WRAP_CONTENT, 1f)
                }
                map[k] = e; r.addView(e)
            }
            root.addView(r)
        }
        val btns = LinearLayout(this)
        fun btn(t: String, f: () -> Unit) = Button(this).apply { text = t; setOnClickListener { f() }
            layoutParams = LinearLayout.LayoutParams(0, WRAP_CONTENT, 1f) }
        btns.addView(btn("Analyse") { run(prefs) })
        btns.addView(btn("Sample") {
            Analyzer.sample.first.forEach { (k, v) -> cur[k]?.setText(v.toLong().toString()) }
            Analyzer.sample.second.forEach { (k, v) -> pri[k]?.setText(v.toLong().toString()) }
            run(prefs) })
        btns.addView(btn("Clear") { (cur.values + pri.values).forEach { it.setText("") }; out.text = "" })
        root.addView(btns)
        out = TextView(this).apply { textSize = 14f; setTextIsSelectable(true); setPadding(0, 16, 0, 16) }
        root.addView(out)
        setContentView(ScrollView(this).apply { addView(root, MATCH_PARENT, WRAP_CONTENT) })
    }

    private fun read(m: Map<String, EditText>) = m.mapNotNull { (k, e) ->
        e.text.toString().replace(",", "").toDoubleOrNull()?.let { k to it } }.toMap()

    private fun run(prefs: android.content.SharedPreferences) {
        val c = read(cur); val p = read(pri)
        prefs.edit().apply {
            cur.forEach { (k, e) -> putString("c_$k", e.text.toString()) }
            pri.forEach { (k, e) -> putString("p_$k", e.text.toString()) }
        }.apply()
        out.text = if (c.isEmpty()) "Enter current-period figures first." else Analyzer.report(c, p)
    }
}
