# Release Notes

## 2.1.3 -- Changes from ElmahCore 2.1.2

### Summary

Drop in replacement for existing ElmahCore with collection of outstanding PR's merged in.

### Changes

- Dropped support for net core 3.1, 5.0 (unsupported from Microsoft)
- Tests run under .NET6 [#162](https://github.com/ElmahCore/ElmahCore/pull/162)

### Fixes

- Increase size of MySQL data length for Message to TEXT and AllXML [#1]
- Updated ELMAHCore to use `Microsoft.Data.SqlClient` [#157](https://github.com/ElmahCore/ElmahCore/pull/163)
- Update other package dependencies
- Fix a stack trace high CPU usage bug [#158](https://github.com/ElmahCore/ElmahCore/pull/164)
- Make ErrorTextFormatter public [#165](https://github.com/ElmahCore/ElmahCore/pull/165)