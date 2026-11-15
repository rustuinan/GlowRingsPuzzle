# Google Play coin shop

The shop opens directly to coin packs and No Ads. A No Ads card precedes three coin-pack cards in a vertical scrolling list. Cards separate title, reward/description and price action. The wallet sits beside the shop title in an icon badge. Text uses a wider Digitalt font with baked Turkish glyphs, additional letter/line spacing and a separate configurable body font. The header wallet and menu wallet share `CosmeticsSave`; the wallet plus button opens the store. Unknown prices/products disable buying; no invented currency prices or editor purchases are used.

`Assets/_Game/Resources/CoinShopConfig.asset` is the product source:

| Google Play product ID | Type | Coins |
| --- | --- | --- |
| `coins_2000` | Consumable one-time product | 2,000 |
| `coins_6000` | Consumable one-time product | 6,000 |
| `coins_15000` | Consumable one-time product | 15,000 |
| `remove_ads` | Non-consumable one-time product | Removes interstitial ads |

These IDs are reserved in the code; the user has not created the Play Console products yet. Create the same IDs in the app's Play Console, set regional prices/availability and activate them. If different IDs are used, update this asset before shipping. `remove_ads` is permanent and must not be configured as a consumable or subscription. It never grants coins.

`NoAdsSave` persists ownership before acknowledging a paid order. Confirmed non-consumable orders from Google Play restore ownership on the same Google account after reinstall. Duplicate deliveries are harmless, and owned products cannot be bought again. An empty/partial purchase-fetch result does not revoke cached ownership, following Unity's documented Google Play restore limitation. The AdMob manager immediately discards loaded interstitials and stops retrying/loading/showing them after ownership is granted; a load callback arriving afterward also discards its ad. Voluntary rewarded ads remain available, as the card explains. [Unity restore-purchases reference](https://docs.unity.com/en-us/iap/restore-purchases).

Unity IAP 5.4.4 is installed. `CoinPurchaseService` persists across scenes, connects to native Google Play on Android, fetches localized product metadata and retrieves unfinished orders on startup/retry. A single purchase at a time can be initiated. Cancellation, deferred payment, disconnection, product-fetch failure and confirmation failure expose separate status messages. Deferred orders do not award coins. Confirmed/consumed history does not re-award coins.

A paid `PendingOrder` is mapped only to catalog products. Transaction ID, receipt presence, quantities and integer bounds are checked. `CoinPurchaseFulfillment` saves through the existing wallet's durable reward journal and acknowledges/consumes only after the wallet and receipt are saved. Re-delivery after interruption is idempotent; a new transaction for the same pack is granted again. `Purchased` is a presentation event, never a currency-grant path. The wallet and consumed-receipt history remain local PlayerPrefs, without cloud restoration or backend receipt verification; reinstalling/clearing local storage does not preserve spent consumable balances.

Real purchase validation still requires a Play-distributed signed Android build, active matching products and a Google Play license tester. Verify cancellation, successful purchase, delayed/pending payment, restart during fulfillment and another purchase of the same pack. No device payment was made by the agent. In the Editor, buying stays disabled and explains that Google Play purchases are available on Android. Link a Unity cloud project/add the Google Play license key in the Unity IAP service settings when enabling Unity's transaction/revenue verification.

The Android minimum API remains 24. Unity 2022.3's custom base Gradle template includes Unity's documented IAP 5.4 compatibility pins for AndroidX Core 1.13.1 and Kotlin 1.8.22. Billing mode is Google Play. This is a client integration; a full signed APK/AAB build and a Play-device payment are separate release checks.

`Shop Readable Turkish SDF` is a baked Digitalt font covering Turkish text; `CoinShop Turkish SDF` supplies missing currency symbols. Turkish words never need mixed fallback letters. Store narrow nonbreaking spaces are normalized to the font's supported nonbreaking space.

Validation: `Tools > Glow Rings > Validate Coin Purchases (Edit Mode)` checks catalog/art, exact grants, repeated consumables, historical receipt protection, unknown/invalid orders, overflow and interrupted wallet recovery while restoring touched saves. `Validate Mobile Screens (Play Mode)` audits eight screen sizes (320×480 through tablet/landscape), with/without safe-area insets, all menu pages, the coin shop, gameplay, rescue and game-over panels. Scroll content is clipped deliberately; modal bounds, buttons and text overflow are checked. Android player scripts were compiled to `Temp/CoinShopAndroidScripts` successfully.

Implementation references: [Unity IAP purchases](https://docs.unity.com/en-us/iap/purchases), [native/local-catalog setup](https://docs.unity.com/en-us/iap/set-up-in-app-purchasing), [Unity 2022.3 Android compatibility](https://docs.unity.com/en-us/iap/troubleshoot-iap-unity-2022).
