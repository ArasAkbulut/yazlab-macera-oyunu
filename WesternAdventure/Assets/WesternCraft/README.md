# WesternCraft — Batının İzinde

Bu klasördeki üç prefab ve beş malzeme bu proje için Unity geometrisiyle üretildi.
Herhangi bir ücretli asset, harici model, doku veya font satın alınmadı/indirilmedi.
Mevcut German Shepherd, kovboy, golem ve çöl paketleri değiştirilmeden kullanılır;
bu paketlerin kendi lisansları geçerliliğini korur. UI fontu Unity'nin yerleşik fontudur.

## Sahne

- `DogOnlyWinchRoom`: oyuncudan alçak, köpekten yüksek giriş; kapalı yanlar ve çatı.
- `IronboundMineGate`: mevcut TunnelGate collider'ı ve açılma olayına bağlı demir kuşaklı ahşap kapı.
- `ProspectorsTreasureChest`: kapak menteşesi, metal kuşaklar, kilit ve altın külçeleri.
- `WesternCraft Set / AdventureFlow`: Türkçe başlangıç, beş adımlı konuşma, duraklatma,
  hedef metni, yenilgi ve dönüşte muhafız ikna edildikten sonra zafer ekranı.

## Oynanış

WASD hareket, Shift koşu, Space zıplama, sol tık yumruk, E etkileşim, Esc duraklatma.
Önce Enemy_01–Enemy_04 rakiplerini yen. Dördü de yenilmeden köpek kol komutunu kabul etmez; görev yazısı sayacı gösterir.
Ardından maden vincinin alçak girişinin yakınına gel, köpek yanına ulaşınca E'ye bas.
Köpek kola gidip kapıyı açar ve geri döner. Golem yenildikten sonra sandığın yanında E'ye bas.
Muhafız boss yaşarken gizlidir; boss yenildiğinde tünelin içinde görünür.
Hazine alınınca oyun devam eder: açılan tünelden geri geç, muhafıza yaklaş ve konuş.
Muhafız yerel Ollama (llama3.2:3b) ile cevap verir. Bağlantı yoksa hata gösterilir; otomatik zafer verilmez.
Köpek 1.2 m, alçak giriş 1.5 m yüksekliğindedir.
Başlangıç konuşması atlanabilir; yeniden oyna sahneyi sıfırlar.

## Companion entegrasyonu

WesternMap'te `CompanionGuide` (NavMesh yolu + CharacterController + kısa engel sensörü)
etkindir. `CompanionAgent` ve DecisionRequester bu sahnede kapalıdır. Orijinal ML-Agents
kodu ve eğitim sahneleri korunmuştur. Aynı köpeği iki hareket sistemi aynı anda sürmez.
Eski, 8 metre yarıçaplı eğitim trigger'ı oyun sahnesinde kapalıdır; kol yalnızca köpek
gerçekten ulaştıktan sonra çalışır. `GameEvents.CompanionClearedObstacle` mevcut kapıyı açar.

## Düzenleme ve kontrol

Prefablar ve sahne normal Unity araçlarıyla düzenlenebilir.
`WesternAdventure > Polish > Build Adventure` kurulum aracıdır; yeniden çalıştırmak
üretilen seti ve konumlarını yeniden oluşturur. Elle düzenledikten sonra gereksiz yere çalıştırma.
`Run Gameplay Checks` Play Mode'da başlangıç/konuşma, rota, geçit, kol, kapı ve sandığı sınar.
Kontrol ayrıca dönüşte kapıdan fiziksel geçişi, muhafızın konuşma kilidini ve sonucun beklenmesini sınar.
Otomatik kontrol muhafızın başarı olayını tetikler; gerçek LLM yanıt kalitesini sınamaz.
Testler sahneyi Play Mode dışında değiştirmez.

## Kanyon sınırları

`Canyon Boundary` sahne grubu dış dağ hattındaki açıklıkları kapatır. Kapı hizasındaki bölücü kaya hattı kuzey ve güney sınırlara birleşir; iki alan arasındaki tek açıklık kapıdır. Taş kemer kapının üzerinden atlamayı engeller. Kayalıkların içindeki yüksek çarpışma sınırları, komşu dağ modellerine tırmanarak harita dışına çıkmayı da önler.

Geometri ve malzemeler proje içinde üretildi; yeni bir ücretli veya harici asset kullanılmadı. NavMesh yeni çarpışma sınırlarıyla yeniden oluşturuldu. Kaya sınırlarının üstü yürünebilir alan sayılmaz.

- Yeniden oluştur: `WesternAdventure > Polish > Seal Canyon Boundaries` (WesternMap açıkken).
- Sınır testi: `WesternAdventure > Polish > Check Canyon Boundaries`. Gerçek CharacterController ile 1.076 geçiş denemesi, kapalı kapının alternatif NavMesh rotası ve açık kapıdan iki yönde geçiş kontrol edilir.
- Tam akış testi: `WesternAdventure > Polish > Run Gameplay Checks`. Köpek, kol, kapı ve muhafıza dönüş akışını kontrol eder.
