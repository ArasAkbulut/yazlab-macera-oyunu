# -*- coding: utf-8 -*-
"""
Muhafız NPC'sinin çok turlu sorgulama testleri (rapor Bölüm 6).

Oyundaki akışı birebir taklit eder:
  - Sistem promptu:  NpcPersona.BuildSystemPrompt  (WesternAdventure/Assets/Scripts/NPC/NpcPersona.cs)
  - Backstory:       Assets/Data/NpcPersonas/TunnelGuardPersona.asset
  - Açılış sorusu:   WesternMap.unity > TunnelGuard > GuardEncounter.openingQuestion
  - Kullanıcı mesajı: konuşmanın soru/cevap dökümü (NpcPersona.BuildTranscript)
  - Karar:           NpcDialogueController — model değerlendirme alanlarını doldurur, kararı kod verir
  - Model ayarları:  OllamaModels.cs (temperature 0.2, format json)

Kullanım:  python training/test_npc_dialogue.py [tekrar_sayısı]
Çıktı:     training/final_npc_test_results.txt
"""
import json
import sys
import time
import urllib.request

MODEL = "llama3.2:3b"
URL = "http://localhost:11434/api/chat"
MAX_TURNS = 3

NPC_NAME = "Tünel Muhafızı"
BACKSTORY = (
    "Sen, Golem'in tünelindeki çıkışı bekleyen sert, güvenmeyen bir muhafızsın. Oyuncu Golem'i yendi, "
    "hazineyi aldı ve tünelden geri dönüyor; sen önünü kestin. Hazineyi gerçekten hak edip etmediğine, "
    "oyuncunun sana anlatacaklarına bakarak sen karar vereceksin. İkna olursan kenara çekilip hazineyle "
    "geçmesine izin verirsin (open_door); olmazsan onu alt edersin (attack_player)."
)
OPENING = ("Dur, yolcu! Golemi yenip hazineyi almışsın. Bu hazineyle ne yapacaksın? "
           "Geçmene izin vermem için beni ikna et.")
QUESTIONS = ["Sana neden güveneyim? Daha önce kimin için ne yaptın?",
             "Sözünü tutacağını nereden bileyim?"]
PASS_LINE = "Sözlerin tutarlı, niyetin temiz. Geç bakalım yolcu, hazine senin."
FAIL_LINE = "Seni dinledim ama ikna olmadım. Bu hazineyle buradan geçemezsin!"
INSULT_LINE = "Bana bu şekilde konuşmanın bedelini ödeyeceksin!"


def build_system_prompt(turn):
    return (
        f"Sen bir western macera oyunundaki NPC'sin. İsmin: {NPC_NAME}.\n"
        f"Karakterin ve amacın: {BACKSTORY}\n"
        f"Oyuncuyu sorguluyorsun. Şu an oyuncunun {turn}. cevabındayız (en fazla {MAX_TURNS}).\n\n"
        "Kurallar:\n"
        "- SADECE aşağıdaki JSON şemasıyla cevap ver, başka hiçbir metin, açıklama veya markdown ekleme.\n"
        "- Şema: {\"hakaret\": true|false, \"yardim_amaci\": true|false, \"guven_ornegi\": true|false, "
        "\"celiski\": true|false, \"alakasiz\": true|false, \"reply\": string, "
        "\"disposition\": \"friendly\"|\"neutral\"|\"hostile\"}\n"
        "- hakaret: Oyuncu sana hakaret etti ya da kaba/saygısız konuştu mu?\n"
        "- yardim_amaci: Oyuncu hazineyi başka insanlara (köy, aile, hastalar vb.) yardım için kullanacağını söyledi mi?\n"
        "- guven_ornegi: Oyuncu geçmişte başka birine yardım ettiği SOMUT bir örnek anlattı mı? "
        "(kimseye yardım etmedim derse false)\n"
        "- celiski: Oyuncu kendi söylediklerini değiştirdi ya da birbiriyle çelişen şeyler söyledi mi? "
        "(örn. önce 'annem için' deyip sonra 'annem yok' demek)\n"
        "- alakasiz: Oyuncunun cevapları saçma, şaka ya da hazineyle hiç ilgisiz mi?\n"
        "- reply: Oyuncunun SON cevabına TEK kısa cümlelik, sert bir muhafıza yakışan bir tepki. "
        "Soru sorma, karar bildirme.\n"
        "- reply SADECE Türkçe kelimelerden oluşmalı; İngilizce kelime kullanma.")


def build_transcript(questions, answers):
    out = "Konuşma:\n"
    for i, a in enumerate(answers):
        if i < len(questions):
            out += f"Muhafız: {questions[i]}\n"
        out += f"Oyuncu: {a}\n"
    return out


# (isim, beklenen karar, oyuncunun sırayla vereceği cevaplar)
SCENARIOS = [
    ("İkna edici ve tutarlı", "open_door", [
        "Köyümüzde iki yıldır kuraklık var, bu altınla köy için derin bir kuyu kazdıracağım.",
        "Geçen kış atımı satıp yaşlı komşumun yıkılan evini tamir ettim, köyde herkes bilir.",
        "Kuyunun başına köyün muhtarını da koyacağız, altını o saklayacak; hesabını herkese vereceğim.",
    ]),
    ("Açgözlü", "attack_player", [
        "Zengin olacağım, kendime büyük bir çiftlik alacağım.",
        "Başkasından bana ne, hazineyi ben buldum.",
        "Hiçbir şey, ver şunu artık.",
    ]),
    ("Hakaret (1. turda)", "attack_player", [
        "Çekil yolumdan, aptal ihtiyar!",
    ]),
    ("Alakasız / saçma", "attack_player", [
        "Bugün hava çok güzel değil mi, atımın adı Yıldız.",
        "Bilmem, belki kaktüs toplarım.",
        "Muz.",
    ]),
    ("Tutarsız hikâye", "attack_player", [
        "Hasta annemin ilaçları için.",
        "Aslında annem yok, kardeşim için yani, ya da kendim için, fark etmez.",
        "Daha önce kimseye yardım etmedim ama bu sefer ederim herhalde.",
    ]),
]

ENGLISH_HINTS = {"the", "and", "because", "reason", "need", "needs", "you", "human", "please", "deep",
                 "place", "only", "thing", "benefit", "benefits", "decision", "someone", "demand",
                 "demands", "but", "pero", "trust", "treasure", "guard", "warning", "about", "our"}


def chat(system_prompt, user_message):
    payload = {"model": MODEL, "stream": False, "format": "json", "options": {"temperature": 0.2},
               "messages": [{"role": "system", "content": system_prompt},
                            {"role": "user", "content": user_message}]}
    req = urllib.request.Request(URL, data=json.dumps(payload).encode("utf-8"),
                                 headers={"Content-Type": "application/json"})
    started = time.time()
    with urllib.request.urlopen(req, timeout=60) as resp:
        body = json.loads(resp.read().decode("utf-8"))
    return body["message"]["content"], time.time() - started


def run_scenario(answers, log):
    questions = [OPENING]
    given = []
    log(f"  Muhafız (açılış): {OPENING}")
    stats = {"turns": 0, "latencies": [], "bad_json": 0, "english": 0}
    decision = "none"
    for turn, answer in enumerate(answers, start=1):
        given.append(answer)
        content, latency = chat(build_system_prompt(turn), build_transcript(questions, given))
        stats["turns"] = turn
        stats["latencies"].append(latency)
        try:
            d = json.loads(content)
        except json.JSONDecodeError:
            stats["bad_json"] += 1
            d = {}
        flags = {k: bool(d.get(k)) for k in ("hakaret", "yardim_amaci", "guven_ornegi", "celiski", "alakasiz")}
        model_reply = str(d.get("reply", "")).strip()
        if any(w.strip(".,!?;:\"'()").lower() in ENGLISH_HINTS for w in model_reply.split()):
            stats["english"] += 1
        convincing = (flags["yardim_amaci"] and flags["guven_ornegi"] and not flags["celiski"]
                      and not flags["alakasiz"] and not flags["hakaret"])
        if flags["hakaret"]:
            decision, shown = "attack_player", INSULT_LINE
        elif turn >= MAX_TURNS:
            decision = "open_door" if convincing else "attack_player"
            shown = PASS_LINE if convincing else FAIL_LINE
        else:
            question = QUESTIONS[min(turn - 1, len(QUESTIONS) - 1)]
            shown = f"{model_reply} {question}".strip()
            questions.append(question)
        flag_text = " ".join(f"{k}={'E' if v else 'H'}" for k, v in flags.items())
        log(f"  Tur {turn} Oyuncu: {answer}")
        log(f"  Tur {turn} Model [{latency:.1f} sn] {flag_text}")
        log(f"  Tur {turn} Muhafız [{decision}]: {shown}")
        if decision != "none":
            break
    return decision, stats


def main():
    sys.stdout.reconfigure(encoding="utf-8")  # Windows konsolu (cp1254)
    repeats = int(sys.argv[1]) if len(sys.argv) > 1 else 3
    lines = []

    def log(text=""):
        print(text, flush=True)
        lines.append(text)

    log(f"Muhafız NPC çok turlu sorgulama testi — model {MODEL}, temperature 0.2, {repeats} tekrar/senaryo")
    log(time.strftime("%Y-%m-%d %H:%M"))
    summary = []
    for name, expected, answers in SCENARIOS:
        correct, lat, turns, english, bad_json = 0, [], [], 0, 0
        for r in range(1, repeats + 1):
            log(f"\n=== {name} — deneme {r}/{repeats} (beklenen: {expected}) ===")
            decision, st = run_scenario(answers, log)
            ok = decision == expected
            correct += ok
            lat += st["latencies"]
            turns.append(st["turns"])
            english += st["english"]
            bad_json += st["bad_json"]
            log(f"  -> Karar: {decision} {'✅' if ok else '❌'}")
        summary.append((name, expected, correct, repeats, sum(turns) / len(turns),
                        sum(lat) / len(lat), max(lat), english, bad_json))

    log("\n## Özet")
    log("| Senaryo | Beklenen | Doğru karar | Ort. tur | Ort. / en uzun yanıt | İngilizce sızıntı (tepki) | Geçersiz JSON |")
    log("|---|---|---|---|---|---|---|")
    for (name, exp, c, n, t, avg, mx, en, bj) in summary:
        log(f"| {name} | `{exp}` | {c}/{n} | {t:.1f} | {avg:.1f} / {mx:.1f} sn | {en} | {bj} |")
    total_c = sum(s[2] for s in summary)
    total_n = sum(s[3] for s in summary)
    log(f"\nToplam doğru karar: {total_c}/{total_n} (%{100 * total_c / total_n:.0f})")

    with open("training/final_npc_test_results.txt", "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")


if __name__ == "__main__":
    main()
