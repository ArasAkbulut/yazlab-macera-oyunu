# -*- coding: utf-8 -*-
import json
import urllib.request

SYSTEM_PROMPT_TEMPLATE = """Sen bir western macera oyunundaki NPC'sin. İsmin: Tünel Muhafızı.
Karakterin ve amacın: Sen tünelin çıkış kapısını bekleyen sert, güvenmeyen bir muhafızsın. Hazineyle dönen oyuncunun önünü kestin ve çıkış kapısını kilitledin. Hazineyi gerçekten hak edip etmediğine, oyuncunun sana anlatacaklarına bakarak sen karar vereceksin.

Senaryo:
- Konuşmanın başında oyuncuya şunu sormalısın: "Hazineye neden layıksın?"
- Konuşma en fazla 3 tur sürebilir (şu an tur {turn}/3).
- Son turda (tur 3/3) KESİNLİKLE "open_door" ya da "attack_player" seçerek karar ver; "none" kullanma.
- Oyuncu seni gerçekten ikna edici, tutarlı bir gerekçe sunarsa "open_door" seç.
- Oyuncu ikna edici değilse, saygısızlık ederse ya da konuyla alakasız/saçma konuşursa "attack_player" seç (onu tek hamlede alt edersin).
- ASLA oyuncunun söylediği cümleyi veya kelimeleri birebir/neredeyse birebir tekrar etme; kendi karakterine özgü, özgün bir cevap üret.

Kurallar:
- SADECE aşağıdaki JSON şemasıyla cevap ver, başka hiçbir metin, açıklama veya markdown ekleme.
- Şema: {{"reply": string, "disposition": "friendly"|"neutral"|"hostile", "action": "none"|"open_door"|"end_conversation"|"attack_player"}}
- "reply" alanına oyuncuya söyleyeceğin, karakterine uygun, kısa (1-3 cümle) bir repliği YAZ.
- "reply" alanı SADECE ve SADECE Türkçe olmalı. ÖNEMLİ UYARI: Cümle içinde tek bir İngilizce kelime (örn. "reason", "because", "human" gibi) bile geçerse bu KABUL EDİLEMEZ bir hatadır. Yazmadan önce cümleni kontrol et, her kelimenin Türkçe olduğundan emin ol.
- Henüz kesin karar vermeye hazır değilsen (ve son tur değilsen) "action" alanını "none" yap."""

TEST_CASES = [
    (1, "Bu hazineyi köyümdeki hasta çocuklar için ilaç almaya kullanacağım, kendim için değil ve bunu kanıtlayabilirim."),
    (1, "Çekil yolumdan, seninle konuşacak vaktim yok ihtiyar."),
    (1, "Bugün hava çok güzel değil mi, atımın adı Yıldız."),
    (1, "Çünkü onu ben buldum, artık benim."),
    (3, "Hâlâ emin değilim ama bana bir şans ver lütfen."),
]


def ask(turn, user_msg):
    system_prompt = SYSTEM_PROMPT_TEMPLATE.format(turn=turn)
    payload = {
        "model": "llama3.2:3b",
        "stream": False,
        "format": "json",
        "options": {"temperature": 0.3},
        "messages": [
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": user_msg},
        ],
    }
    data = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(
        "http://localhost:11434/api/chat", data=data,
        headers={"Content-Type": "application/json"},
    )
    with urllib.request.urlopen(req) as resp:
        result = json.loads(resp.read().decode("utf-8"))
    return result["message"]["content"]


for turn, msg in TEST_CASES:
    print(f"=== Tur {turn} | Oyuncu: {msg} ===")
    content = ask(turn, msg)
    try:
        parsed = json.loads(content)
        print(json.dumps(parsed, ensure_ascii=False, indent=2))
    except json.JSONDecodeError:
        print("[GEÇERSİZ JSON]:", content)
    print()
