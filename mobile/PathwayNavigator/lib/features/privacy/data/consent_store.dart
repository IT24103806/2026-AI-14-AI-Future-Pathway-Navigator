import '../../../core/storage/key_value_store.dart';

/// Records that the user has read the privacy & AI-use notice.
///
/// The key embeds the notice version: bumping [currentVersion] after a material change to the
/// notice makes every user re-confirm it (informed consent must stay current).
class ConsentStore {
  ConsentStore(this._store);

  static const int currentVersion = 1;
  static const String _key = 'privacy_consent_version';

  final KeyValueStore _store;

  Future<bool> hasAccepted() async {
    try {
      return await _store.read(_key) == '$currentVersion';
    } on Exception {
      return false; // unreadable store: ask again rather than assume consent
    }
  }

  Future<void> accept() => _store.write(_key, '$currentVersion');

  Future<void> withdraw() => _store.delete(_key);
}
