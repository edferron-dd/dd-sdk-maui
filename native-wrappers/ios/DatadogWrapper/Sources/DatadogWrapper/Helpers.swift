import Foundation

// Converts an NSDictionary (from ObjC/C# boundary) to [String: any Encodable]
// for use with dd-sdk-ios 3.x APIs that require Encodable values.
func makeEncodableDict(_ dict: [String: Any]) -> [String: any Encodable] {
    dict.reduce(into: [:]) { result, pair in
        result[pair.key] = makeEncodable(pair.value)
    }
}

func makeEncodable(_ value: Any) -> any Encodable {
    switch value {
    case let s as String:
        return s
    case let n as NSNumber:
        if CFGetTypeID(n) == CFBooleanGetTypeID() {
            return n.boolValue
        }
        // Prefer Int when no fractional part
        let d = n.doubleValue
        if d == d.rounded() && !d.isInfinite, let i = Int(exactly: d) {
            return i
        }
        return d
    default:
        return "\(value)"
    }
}
