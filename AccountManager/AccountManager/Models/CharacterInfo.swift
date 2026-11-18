import Foundation

/// Live character data received from the game client.
struct CharacterInfo: Codable, Equatable {
    private static let iso8601Lock = NSLock()

    private static let iso8601FractionalFormatter: ISO8601DateFormatter = {
        let formatter = ISO8601DateFormatter()
        formatter.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        return formatter
    }()

    private static let iso8601StandardFormatter: ISO8601DateFormatter = {
        let formatter = ISO8601DateFormatter()
        formatter.formatOptions = [.withInternetDateTime]
        return formatter
    }()

    private static func parseISO8601Date(_ string: String) -> Date? {
        iso8601Lock.lock()
        defer { iso8601Lock.unlock() }
        return iso8601FractionalFormatter.date(from: string)
            ?? iso8601StandardFormatter.date(from: string)
    }

    var lastUpdated: Date = Date()

    // Status
    var status: String = ""

    // Character info
    var cName: String = ""
    var cgender: Int = 0

    // Map info
    var mapName: String = ""
    var mapID: Int = 0
    var zoneID: Int = 0

    // Position
    var cx: Int = 0
    var cy: Int = 0

    // Character stats
    var cHP: Int64 = 0
    var cHPFull: Int64 = 0
    var cMP: Int64 = 0
    var cMPFull: Int64 = 0
    var cStamina: Int = 0
    var cPower: Int64 = 0
    var cTiemNang: Int64 = 0

    // Base stats
    var cHPGoc: Int = 0
    var cMPGoc: Int = 0
    var cDefGoc: Int = 0
    var cDamGoc: Int = 0
    var cCriticalGoc: Int = 0

    // Full stats
    var cDamFull: Int64 = 0
    var cDefull: Int64 = 0
    var cCriticalFull: Int = 0

    // Pet stats
    var cPetHP: Int64 = 0
    var cPetHPFull: Int64 = 0
    var cPetMP: Int64 = 0
    var cPetMPFull: Int64 = 0
    var cPetStamina: Int = 0
    var cPetPower: Int64 = 0
    var cPetTiemNang: Int64 = 0
    var cPetDamFull: Int64 = 0
    var cPetDefull: Int64 = 0
    var cPetCriticalFull: Int = 0

    // Currency
    var xu: Int64 = 0
    var luong: Int = 0
    var luongKhoa: Int = 0

    private enum CodingKeys: String, CodingKey {
        case lastUpdated
        case status
        case cName, cgender
        case mapName, mapID, zoneID
        case cx, cy
        case cHP, cHPFull, cMP, cMPFull, cStamina, cPower, cTiemNang
        case cHPGoc, cMPGoc, cDefGoc, cDamGoc, cCriticalGoc
        case cDamFull, cDefull, cCriticalFull
        case cPetHP, cPetHPFull, cPetMP, cPetMPFull, cPetStamina, cPetPower, cPetTiemNang, cPetDamFull, cPetDefull, cPetCriticalFull
        case xu, luong, luongKhoa
    }

    init() {}

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)

        if let rawDate = try? container.decode(String.self, forKey: .lastUpdated),
           let parsedDate = Self.parseISO8601Date(rawDate) {
            self.lastUpdated = parsedDate
        } else {
            self.lastUpdated = (try? container.decode(Date.self, forKey: .lastUpdated)) ?? Date()
        }

        self.status          = try container.decodeIfPresent(String.self, forKey: .status) ?? ""
        self.cName           = try container.decodeIfPresent(String.self, forKey: .cName) ?? ""
        self.cgender         = try container.decodeIfPresent(Int.self, forKey: .cgender) ?? 0
        self.mapName         = try container.decodeIfPresent(String.self, forKey: .mapName) ?? ""
        self.mapID           = try container.decodeIfPresent(Int.self, forKey: .mapID) ?? 0
        self.zoneID          = try container.decodeIfPresent(Int.self, forKey: .zoneID) ?? 0
        self.cx              = try container.decodeIfPresent(Int.self, forKey: .cx) ?? 0
        self.cy              = try container.decodeIfPresent(Int.self, forKey: .cy) ?? 0
        self.cHP             = try container.decodeIfPresent(Int64.self, forKey: .cHP) ?? 0
        self.cHPFull         = try container.decodeIfPresent(Int64.self, forKey: .cHPFull) ?? 0
        self.cMP             = try container.decodeIfPresent(Int64.self, forKey: .cMP) ?? 0
        self.cMPFull         = try container.decodeIfPresent(Int64.self, forKey: .cMPFull) ?? 0
        self.cStamina        = try container.decodeIfPresent(Int.self, forKey: .cStamina) ?? 0
        self.cPower          = try container.decodeIfPresent(Int64.self, forKey: .cPower) ?? 0
        self.cTiemNang       = try container.decodeIfPresent(Int64.self, forKey: .cTiemNang) ?? 0
        self.cHPGoc          = try container.decodeIfPresent(Int.self, forKey: .cHPGoc) ?? 0
        self.cMPGoc          = try container.decodeIfPresent(Int.self, forKey: .cMPGoc) ?? 0
        self.cDefGoc         = try container.decodeIfPresent(Int.self, forKey: .cDefGoc) ?? 0
        self.cDamGoc         = try container.decodeIfPresent(Int.self, forKey: .cDamGoc) ?? 0
        self.cCriticalGoc    = try container.decodeIfPresent(Int.self, forKey: .cCriticalGoc) ?? 0
        self.cDamFull        = try container.decodeIfPresent(Int64.self, forKey: .cDamFull) ?? 0
        self.cDefull         = try container.decodeIfPresent(Int64.self, forKey: .cDefull) ?? 0
        self.cCriticalFull   = try container.decodeIfPresent(Int.self, forKey: .cCriticalFull) ?? 0
        self.cPetHP          = try container.decodeIfPresent(Int64.self, forKey: .cPetHP) ?? 0
        self.cPetHPFull      = try container.decodeIfPresent(Int64.self, forKey: .cPetHPFull) ?? 0
        self.cPetMP          = try container.decodeIfPresent(Int64.self, forKey: .cPetMP) ?? 0
        self.cPetMPFull      = try container.decodeIfPresent(Int64.self, forKey: .cPetMPFull) ?? 0
        self.cPetStamina     = try container.decodeIfPresent(Int.self, forKey: .cPetStamina) ?? 0
        self.cPetPower       = try container.decodeIfPresent(Int64.self, forKey: .cPetPower) ?? 0
        self.cPetTiemNang    = try container.decodeIfPresent(Int64.self, forKey: .cPetTiemNang) ?? 0
        self.cPetDamFull     = try container.decodeIfPresent(Int64.self, forKey: .cPetDamFull) ?? 0
        self.cPetDefull      = try container.decodeIfPresent(Int64.self, forKey: .cPetDefull) ?? 0
        self.cPetCriticalFull = try container.decodeIfPresent(Int.self, forKey: .cPetCriticalFull) ?? 0
        self.xu              = try container.decodeIfPresent(Int64.self, forKey: .xu) ?? 0
        self.luong           = try container.decodeIfPresent(Int.self, forKey: .luong) ?? 0
        self.luongKhoa       = try container.decodeIfPresent(Int.self, forKey: .luongKhoa) ?? 0
    }

    // MARK: - Computed helpers

    var genderString: String {
        switch cgender {
        case 0: return "Trái Đất"
        case 1: return "Namek"
        case 2: return "Xayda"
        default: return "Unknown"
        }
    }

    var hpPercent: Double {
        cHPFull > 0 ? Double(cHP) / Double(cHPFull) * 100 : 0
    }

    var mpPercent: Double {
        cMPFull > 0 ? Double(cMP) / Double(cMPFull) * 100 : 0
    }

    var petHPPercent: Double {
        cPetHPFull > 0 ? Double(cPetHP) / Double(cPetHPFull) * 100 : 0
    }

    var petMPPercent: Double {
        cPetMPFull > 0 ? Double(cPetMP) / Double(cPetMPFull) * 100 : 0
    }

    static func from(_ dict: [String: Any]) -> CharacterInfo {
        var info = CharacterInfo()
        if let rawDate = dict["lastUpdated"] as? String,
           let parsedDate = parseISO8601Date(rawDate) {
            info.lastUpdated = parsedDate
        } else {
            info.lastUpdated = Date()
        }

        info.status         = dict["status"] as? String ?? ""
        info.cName          = dict["cName"] as? String ?? ""
        info.cgender        = (dict["cgender"] as? NSNumber)?.intValue ?? 0

        info.mapName        = dict["mapName"] as? String ?? ""
        info.mapID          = (dict["mapID"] as? NSNumber)?.intValue ?? 0
        info.zoneID         = (dict["zoneID"] as? NSNumber)?.intValue ?? 0

        info.cx             = (dict["cx"] as? NSNumber)?.intValue ?? 0
        info.cy             = (dict["cy"] as? NSNumber)?.intValue ?? 0

        info.cHP            = (dict["cHP"] as? NSNumber)?.int64Value ?? 0
        info.cHPFull        = (dict["cHPFull"] as? NSNumber)?.int64Value ?? 0
        info.cMP            = (dict["cMP"] as? NSNumber)?.int64Value ?? 0
        info.cMPFull        = (dict["cMPFull"] as? NSNumber)?.int64Value ?? 0
        info.cStamina       = (dict["cStamina"] as? NSNumber)?.intValue ?? 0
        info.cPower         = (dict["cPower"] as? NSNumber)?.int64Value ?? 0
        info.cTiemNang      = (dict["cTiemNang"] as? NSNumber)?.int64Value ?? 0

        info.cHPGoc         = (dict["cHPGoc"] as? NSNumber)?.intValue ?? 0
        info.cMPGoc         = (dict["cMPGoc"] as? NSNumber)?.intValue ?? 0
        info.cDefGoc        = (dict["cDefGoc"] as? NSNumber)?.intValue ?? 0
        info.cDamGoc        = (dict["cDamGoc"] as? NSNumber)?.intValue ?? 0
        info.cCriticalGoc   = (dict["cCriticalGoc"] as? NSNumber)?.intValue ?? 0

        info.cDamFull       = (dict["cDamFull"] as? NSNumber)?.int64Value ?? 0
        info.cDefull        = (dict["cDefull"] as? NSNumber)?.int64Value ?? 0
        info.cCriticalFull  = (dict["cCriticalFull"] as? NSNumber)?.intValue ?? 0

        info.cPetHP         = (dict["cPetHP"] as? NSNumber)?.int64Value ?? 0
        info.cPetHPFull     = (dict["cPetHPFull"] as? NSNumber)?.int64Value ?? 0
        info.cPetMP         = (dict["cPetMP"] as? NSNumber)?.int64Value ?? 0
        info.cPetMPFull     = (dict["cPetMPFull"] as? NSNumber)?.int64Value ?? 0
        info.cPetStamina    = (dict["cPetStamina"] as? NSNumber)?.intValue ?? 0
        info.cPetPower      = (dict["cPetPower"] as? NSNumber)?.int64Value ?? 0
        info.cPetTiemNang   = (dict["cPetTiemNang"] as? NSNumber)?.int64Value ?? 0
        info.cPetDamFull    = (dict["cPetDamFull"] as? NSNumber)?.int64Value ?? 0
        info.cPetDefull     = (dict["cPetDefull"] as? NSNumber)?.int64Value ?? 0
        info.cPetCriticalFull = (dict["cPetCriticalFull"] as? NSNumber)?.intValue ?? 0

        info.xu             = (dict["xu"] as? NSNumber)?.int64Value ?? 0
        info.luong          = (dict["luong"] as? NSNumber)?.intValue ?? 0
        info.luongKhoa      = (dict["luongKhoa"] as? NSNumber)?.intValue ?? 0

        return info
    }
}