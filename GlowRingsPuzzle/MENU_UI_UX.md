# Ring Match — Mobil menü ve ekran düzeni

## Ana sayfa

- Üst alan: kompakt coin alanı, zeminli +, kupa ve ayarlar. Rekor, başlığın altında küçük bir rozet içindedir.
- Oyun kimliği: Ring Match adı, gerçek modellerden perspektifli ve bağımsız hareketli 3D halkalar, çevrelerinde dönen küçük halkalar ve oyunun arka plan shader'ı.
- Birincil aksiyon: Highscore Play. Hemen altında ayrı bir Puzzle Mode girişi bulunur.
- Alt üçlü alan: günlük ödül, günlük çark ve mağaza. Günlük bulmaca yoktur.
- Play ve alt eylemler kullanılabilir yüksekliğe göre aşağıda tutulur; kısa ekranlarda kaydırma korunur.
- Highscore Play, rekor ve ses/müzik ayarları çalışır. Henüz geliştirilmemiş içerikler açıkça Coming Soon veya Soon olarak gösterilir; ödül veya satın alma işlemi taklit edilmez.

## Ekran davranışı

- Ana menü cihazın güvenli alanına uyar; kısa/yatay ekranlarda içerik dikey kaydırılır.
- Menü pencereleri güvenli alana sığan ortak bir tasarım çerçevesi kullanır.
- Game, Game Over, booster penceresi, tutorial ve geri bildirimler 1080×1920 referans çerçevesini paylaşır. Oyun kamerası aynı çerçeveye hizalanır; çevresi oyun shader'ıyla doldurulur.
- Ekran boyutu ve güvenli alan değişiklikleri çalışma sırasında uygulanır.

## Doğrulama

Unity Editor'de 320×568, 360×640, 390×844, 412×915, 768×1024, 1024×1366 ve 1920×1080 boyutları kontrol edildi. Menü için normal ve çentik simülasyonu, oyun ekranları için güvenli alan simülasyonu: toplam 21 senaryo.

Buton sınırları, metin taşmaları, kaydırma erişimi, tüm menü pencereleri, ses ayarları, Play geçişi, oyun, booster seçimi ve Game Over kontrol edildi. Bu, fiziksel cihaz testinin yerine geçmeyen Editor doğrulamasıdır.

Tekrar çalıştırmak için MainMenu'de Play Mode'a girip `Tools > Glow Rings > Validate Mobile Screens (Play Mode)` seçilir. Kurulum araçları `MobileMenuSetup.cs`, sayfa yönlendirmeleri `MainMenuUI.cs` ve `MenuActionButton.cs` içindedir.
