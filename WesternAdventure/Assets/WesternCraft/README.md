# WesternCraft — Batının İzinde

Bu klasördeki üç prefab ve beş malzeme bu proje için Unity geometrisiyle üretildi.
Herhangi bir ücretli asset, harici model, doku veya font satın alınmadı/indirilmedi.
Mevcut German Shepherd, kovboy, golem ve çöl paketleri değiştirilmeden kullanılır;
bu paketlerin kendi lisansları geçerliliğini korur. UI fontu Unity'nin yerleşik fontudur.

## Sahne

- `DogOnlyWinchRoom`: oyuncudan alçak, köpekten yüksek giriş; kapalı yanlar ve çatı.
- `IronboundMineGate`: mevcut TunnelGate collider'ı ve açılma olayına bağlı demir kuşaklı ahşap kapı.
- `ProspectorsTreasureChest`: kapak menteşesi, metal kuşaklar, kilit ve altın külçeleri.
- `WesternCraft Set / AdventureFlow`: Türkçe başlangıç, dört adımlı konuşma, duraklatma,
  hedef metni, yenilgi ve sandık açıldıktan sonra zafer ekranı.

## Oynanış

WASD hareket, Shift koşu, Space zıplama, sol tık yumruk, E etkileşim, Esc duraklatma.
Maden vincinin alçak girişinin yakınına gel, köpek yanına ulaşınca E'ye bas.
Köpek kola gidip kapıyı açar ve geri döner. Golem yenildikten sonra sandığın yanında E'ye bas.
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
Testler sahneyi Play Mode dışında değiştirmez.
