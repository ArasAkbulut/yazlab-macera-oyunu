# yazlab-macera-oyunu

Yazılım Geliştirme Laboratuvarı I — Unity macera oyunu

**Ekip:** _(isimler eklenecek)_
**Tarih:** _(teslim tarihi: 6 Kasım 2026)_

## 1. Giriş

_(Problemin tanımı, amaç ve genel oyun senaryosu buraya yazılacak.)_

Proje, oyuncuya eşlik eden otonom bir yapay zekâ companion (ML-Agents tabanlı pekiştirmeli
öğrenme ile fiziksel bir engeli aşar) ile yerel bir LLM (Ollama) üzerinden çalışan, serbest
metinle konuşulabilen bir NPC'yi tek bir 2D macera oyununda birleştirir.

## 2. Otonom Companion AI (ML-Agents)

_(Ödül fonksiyonu, gözlem/aksiyon uzayı, eğitim süreci ve akış diyagramı — Mermaid — buraya.)_

İlgili kod: `WesternAdventure/Assets/Scripts/Companion/`

## 3. Yerel LLM Entegrasyonu

NPC, [Ollama](https://ollama.com/) üzerinde çalışan yerel bir modelle (ör. `llama3.2:1b`)
konuşur. Model çıktısı, oyuncuya gösterilecek repliğin yanında oyun durumunu değiştirecek
bir eylemi de içeren yapılandırılmış JSON formatındadır:

```json
{
  "reply": "Parolayı bilmiyorsan buradan geçemezsin, yabancı.",
  "disposition": "neutral",
  "action": "none"
}
```

- `reply`: oyuncuya gösterilecek, karaktere uygun Türkçe replik.
- `disposition`: `friendly` | `neutral` | `hostile`
- `action`: `none` | `open_door` | `end_conversation`

İlgili kod: `WesternAdventure/Assets/Scripts/NPC/` (`OllamaClient`, `NpcDialogueController`,
`NpcPersona`, `NpcAction`).

_(İstek/yanıt akışının sequence diyagramı — Mermaid — buraya eklenecek.)_

## 4. Hata Yönetimi

Ollama sunucusuna ulaşılamaması, zaman aşımı (`OllamaConnectionException`) veya modelin
geçersiz/bozuk JSON üretmesi (`OllamaParseException`) ayrı ayrı yakalanır; her durumda oyun
çökmeden kullanıcıya anlamlı bir mesaj gösterilir (bkz. `NpcDialogueController`).

## 5. Kurulum

### Ollama
1. [ollama.com](https://ollama.com/) üzerinden Ollama'yı kurun.
2. Kullanılacak modeli indirin: `ollama pull llama3.2:1b` (veya Phi-3 Mini / Gemma-2-2B).
3. Ollama sunucusunun `http://localhost:11434` adresinde çalıştığını doğrulayın.

### ML-Agents (companion eğitimi için)
1. Python 3.10+ ortamında: `pip install mlagents`
2. Unity Editor'de proje ilk açıldığında Package Manager, `Packages/manifest.json`
   içindeki `com.unity.ml-agents` paketini GitHub'dan otomatik indirir (internet gerekir).
3. Eğitim: `mlagents-learn <config.yaml> --run-id=companion-v1`

## 6. Test Sonuçları

_(Otonom ajan başarı oranı, ortalama tamamlama süresi; en az 5 örnek NPC diyaloğu buraya.)_

## 7. Sonuç ve Tartışma

_(Başarılar, sınırlılıklar, olası geliştirmeler.)_
