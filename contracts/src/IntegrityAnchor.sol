// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

/**
 * @title IntegrityAnchor
 * @notice UzayBank işlem zincirlerinin son hash'ini blockchain'e sabitler.
 *
 * AMAÇ:
 * Uygulamanın veritabanındaki işlem kayıtları SHA-256 ile zincirleniyor.
 * Ancak veritabanına erişimi olan biri zinciri baştan yeniden hesaplayarak
 * tutarlı hale getirebilir. Bu kontrat, zincirin son halkasını veritabanının
 * DIŞINA — değiştirilemez bir yere — yazar.
 *
 * Ham işlem verisi ASLA buraya yazılmaz; yalnızca parmak izi (hash).
 * Hem gizlilik hem maliyet açısından gerekli.
 */
contract IntegrityAnchor {

    /**
     * Bir hesabın sabitlenmiş son durumu.
     */
    struct Anchor {
        bytes32 hash;       // zincirin son hash'i
        uint256 timestamp;  // ne zaman yazıldığı (blok zamanı)
        bool exists;        // bu hesap için kayıt var mı
    }

    /**
     * Hesap numarası → sabitlenmiş durum.
     * Solidity'de mapping, C#'taki Dictionary'ye karşılık gelir.
     */
    mapping(uint256 => Anchor) private anchors;

    /**
     * Kontratı kuran adres. Yalnızca bu adres yazabilir.
     *
     * NEDEN: Herkes yazabilseydi, saldırgan hem veritabanını değiştirip
     * hem de zincire kendi uydurma hash'ini yazabilirdi — koruma anlamsız olurdu.
     */
    address public immutable owner;

    /**
     * Zincire yazma olayı (event).
     *
     * Event'ler blockchain'e kaydedilen ve dışarıdan dinlenebilen bildirimlerdir.
     * Uygulamamız bunları dinleyerek "hangi hesap ne zaman sabitlendi" bilgisini
     * takip edebilir. Saklama maliyeti normal veriden çok daha düşüktür.
     */
    event Anchored(
        uint256 indexed accountId,
        bytes32 hash,
        uint256 timestamp
    );

    /**
     * Constructor — kontrat blockchain'e yüklenirken bir kez çalışır.
     * Kontratı yükleyen adres otomatik olarak sahip olur.
     */
    constructor() {
        owner = msg.sender;
    }

    /**
     * Yalnızca sahibin çağırabileceği fonksiyonlar için kontrol.
     *
     * modifier, C#'taki attribute'a benzer: fonksiyonun başına eklenir,
     * fonksiyon çalışmadan önce bu kod yürütülür.
     * "_;" işareti fonksiyonun asıl gövdesinin çalışacağı yeri gösterir.
     */
    modifier onlyOwner() {
        require(msg.sender == owner, "Yalnizca sahip yazabilir");
        _;
    }

    /**
     * Bir hesabın son hash'ini zincire sabitler.
     *
     * Aynı hesap için tekrar çağrılırsa önceki değerin ÜZERİNE yazar.
     * Bu bilinçli: her zaman "en güncel durum" tutuluyor. Geçmiş kayıtlar
     * event'ler sayesinde yine izlenebilir kalıyor.
     *
     * @param accountId Hesap numarası
     * @param hash Zincirin son hash'i (SHA-256, 32 bayt)
     */
    function anchor(uint256 accountId, bytes32 hash) external onlyOwner {
        require(hash != bytes32(0), "Hash bos olamaz");

        anchors[accountId] = Anchor({
            hash: hash,
            timestamp: block.timestamp,
            exists: true
        });

        emit Anchored(accountId, hash, block.timestamp);
    }

    /**
     * Bir hesabın sabitlenmiş hash'ini okur.
     *
     * "view" işareti: bu fonksiyon zincire hiçbir şey yazmaz, yalnızca okur.
     * Okuma işlemleri ücretsizdir ve anında sonuç döner — yazma gibi
     * blok onayı beklemez.
     *
     * @return hash Sabitlenmiş hash (kayıt yoksa sıfır)
     * @return timestamp Yazılma zamanı
     * @return exists Kayıt var mı
     */
    function getAnchor(uint256 accountId)
        external
        view
        returns (bytes32 hash, uint256 timestamp, bool exists)
    {
        Anchor memory a = anchors[accountId];
        return (a.hash, a.timestamp, a.exists);
    }
}