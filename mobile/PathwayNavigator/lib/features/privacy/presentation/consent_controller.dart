import 'package:flutter/foundation.dart';

import '../data/consent_store.dart';

class ConsentController extends ChangeNotifier {
  ConsentController(this._store);

  final ConsentStore _store;
  bool _accepted = false;
  bool _loaded = false;

  bool get accepted => _accepted;

  /// False until the stored decision has been read (the router shows a splash until then).
  bool get isLoaded => _loaded;

  Future<void> load() async {
    _accepted = await _store.hasAccepted();
    _loaded = true;
    notifyListeners();
  }

  Future<void> accept() async {
    await _store.accept();
    _accepted = true;
    notifyListeners();
  }

  Future<void> withdraw() async {
    await _store.withdraw();
    _accepted = false;
    notifyListeners();
  }
}
