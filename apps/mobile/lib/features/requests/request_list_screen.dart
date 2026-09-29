import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../data/models.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import '../profile/edit_request_screen.dart';
import 'request_detail_screen.dart';
import 'request_tile.dart';


enum RequestScope { mine, pending, history }


Future<bool> decideRequests(BuildContext context, List<String> ids, Decision decision) async {
  final s = S.of(context);
  final app = AppScope.read(context);
  String? comment;
  if (decision == Decision.reject) {
    comment = await askReason(
      context,
      title: ids.length == 1 ? s.rejectTitle : s.rejectManyTitle(ids.length),
      subtitle: s.rejectReasonHint,
    );
    if (comment == null) return false;
  } else {
    final ok = await confirmDialog(
      context,
      title: ids.length == 1 ? s.approveTitle : s.approveManyTitle(ids.length),
      body: s.approveBody,
      confirmLabel: s.approve,
      icon: Icons.task_alt_rounded,
    );
    if (!ok) return false;
  }
  try {
    await app.repo.decide(ids, decision, comment: comment);
    if (context.mounted) {
      showToast(context, decision == Decision.approve ? s.approvedToast(ids.length) : s.rejectedToast(ids.length));
    }
    app.refreshBadges();
    return true;
  } catch (e) {
    if (context.mounted) showErrorToast(context, e);
    return false;
  }
}

class RequestListScreen extends StatefulWidget {
  const RequestListScreen({super.key, required this.scope});
  final RequestScope scope;

  @override
  State<RequestListScreen> createState() => _RequestListScreenState();
}

class _RequestListScreenState extends State<RequestListScreen> {
  final _view = GlobalKey<AsyncViewState<List<ApprovalRequest>>>();
  final _search = TextEditingController();
  String? _type;
  ApprovalStatus? _status;
  Set<String>? _selected;

  bool get _pending => widget.scope == RequestScope.pending;

  Future<List<ApprovalRequest>> _load() {
    final repo = AppScope.read(context).repo;
    return switch (widget.scope) {
      RequestScope.mine => repo.myRequests(),
      RequestScope.pending => repo.pendingApprovals(),
      RequestScope.history => repo.approvalHistory(),
    };
  }

  Future<void> _reload() async {
    setState(() => _selected = _selected == null ? null : <String>{});
    await _view.currentState?.reload();
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  List<ApprovalRequest> _apply(List<ApprovalRequest> all) {
    final q = _search.text.trim().toLowerCase();
    return all.where((r) {
      if (_type != null && r.requestType != _type) return false;
      if (_status != null && r.status != _status) return false;
      if (q.isEmpty) return true;
      return r.requestNo.toLowerCase().contains(q) ||
          r.requestedBy.toLowerCase().contains(q) ||
          r.summary.toLowerCase().contains(q);
    }).toList();
  }


  void _setFilter(VoidCallback f) => setState(() {
        f();
        if (_selected != null) _selected = <String>{};
      });

  Future<void> _open(ApprovalRequest r) async {
    if (_selected != null) {
      setState(() => _selected!.contains(r.id) ? _selected!.remove(r.id) : _selected!.add(r.id));
      return;
    }
    await pushPage<void>(context, RequestDetailScreen(id: r.id));
    await _reload();
  }

  Future<void> _decide(List<String> ids, Decision d) async {
    if (await decideRequests(context, ids, d)) {
      setState(() => _selected = null);
      await _reload();
    }
  }

  Future<void> _newRequest() async {
    final s = S.of(context);
    final picked = await showModalBottomSheet<int>(
      context: context,
      builder: (ctx) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(IdaSpace.s4, 0, IdaSpace.s4, IdaSpace.s4),
          child: Column(mainAxisSize: MainAxisSize.min, children: [
            ListTile(
              leading: IconBox(Icons.badge_outlined, size: IdaSizes.avatarSm),
              title: Text(s.editProfileTitle),
              onTap: () => Navigator.pop(ctx, 0),
            ),
            ListTile(
              leading: IconBox(Icons.account_balance_outlined, size: IdaSizes.avatarSm),
              title: Text(s.editBankTitle),
              onTap: () => Navigator.pop(ctx, 1),
            ),
          ]),
        ),
      ),
    );
    if (picked == null || !mounted) return;
    await pushPage<void>(context, picked == 0 ? const EditProfileRequestScreen() : const EditBankRequestScreen());
    await _reload();
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final app = AppScope.of(context);
    final title = switch (widget.scope) {
      RequestScope.mine => s.myRequestsTitle,
      RequestScope.pending => s.pendingTitle,
      RequestScope.history => s.historyTitle,
    };
    final selecting = _selected != null;

    return Scaffold(
      appBar: AppBar(
        title: Text(selecting ? s.selectedCount(_selected!.length) : title),
        leading: selecting
            ? IconButton(
                tooltip: s.cancel,
                icon: const Icon(Icons.close_rounded),
                onPressed: () => setState(() => _selected = null),
              )
            : null,
        actions: [
          if (_pending && app.requireSession.canApprove)
            TextButton(
              key: const Key('select-mode'),
              style: TextButton.styleFrom(foregroundColor: IdaColors.textInverse),
              onPressed: () => setState(() => _selected = selecting ? null : <String>{}),
              child: Text(selecting ? s.done : s.select),
            ),
        ],
      ),
      floatingActionButton: widget.scope == RequestScope.mine && app.requireSession.isDoctor
          ? FloatingActionButton.extended(
              onPressed: _newRequest,
              icon: const Icon(Icons.add_rounded),
              label: Text(s.requestEdit),
            )
          : null,
      body: AsyncView<List<ApprovalRequest>>(
        key: _view,
        load: _load,
        reloadKey: app.dataVersion,
        builder: (context, all, reload) {
          final items = _apply(all);
          final types = <String, int>{};
          for (final r in all) {
            types[r.requestType] = (types[r.requestType] ?? 0) + 1;
          }
          final filtered = _type != null || _status != null || _search.text.isNotEmpty;

          return Column(children: [
            if (all.isNotEmpty) _Filters(
              scope: widget.scope,
              search: _search,
              types: types,
              type: _type,
              status: _status,
              onSearch: () => _setFilter(() {}),
              onType: (t) => _setFilter(() => _type = t),
              onStatus: (v) => _setFilter(() => _status = v),
            ),
            if (selecting && items.isNotEmpty)
              _SelectAllBar(
                all: items.every((r) => _selected!.contains(r.id)),
                some: items.any((r) => _selected!.contains(r.id)),
                onChanged: (v) => setState(() {
                  v ? _selected!.addAll(items.map((r) => r.id)) : _selected!.clear();
                }),
              ),
            Expanded(
              child: RefreshIndicator(
                onRefresh: reload,
                child: items.isEmpty
                    ? ListView(children: [
                        filtered
                            ? EmptyState(
                                icon: Icons.filter_alt_off_outlined,
                                title: s.noMatch,
                                action: s.clearFilter,
                                onAction: () => _setFilter(() {
                                  _type = null;
                                  _status = null;
                                  _search.clear();
                                }),
                              )
                            : switch (widget.scope) {
                                RequestScope.pending => EmptyState(
                                    icon: Icons.task_alt_rounded, title: s.emptyPending, message: s.emptyPendingBody),
                                RequestScope.mine => EmptyState(
                                    icon: Icons.outbox_outlined, title: s.emptyMine, message: s.emptyMineBody),
                                RequestScope.history => EmptyState(
                                    icon: Icons.manage_history_rounded, title: s.emptyHistory, message: s.emptyHistoryBody),
                              },
                      ])
                    : ListView.separated(
                        padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s2, IdaSpace.s4, IdaSpace.s16 + IdaSpace.s4),
                        itemCount: items.length,
                        separatorBuilder: (_, _) => const SizedBox(height: IdaSpace.s3),
                        itemBuilder: (context, i) {
                          final r = items[i];
                          final canQuick = _pending && !selecting && app.requireSession.canApprove;
                          return ContentWidth(
                            child: RequestTile(
                              key: Key('req-${r.id}'),
                              request: r,
                              showRequester: widget.scope != RequestScope.mine,
                              selected: selecting ? _selected!.contains(r.id) : null,
                              onTap: () => _open(r),
                              onApprove: canQuick ? () => _decide([r.id], Decision.approve) : null,
                              onReject: canQuick ? () => _decide([r.id], Decision.reject) : null,
                            ),
                          );
                        },
                      ),
              ),
            ),
          ]);
        },
      ),
      bottomNavigationBar: !selecting
          ? null
          : BottomActionBar(
              child: Row(children: [
                Expanded(
                  child: OutlinedButton.icon(
                    style: OutlinedButton.styleFrom(
                      foregroundColor: context.ida.dangerText,
                      side: BorderSide(color: context.ida.dangerText, width: 1.5),
                    ),
                    onPressed: _selected!.isEmpty ? null : () => _decide(_selected!.toList(), Decision.reject),
                    icon: const Icon(Icons.close_rounded, size: IdaSizes.iconMd),
                    label: Text(s.reject),
                  ),
                ),
                const SizedBox(width: IdaSpace.s3),
                Expanded(
                  child: PrimaryButton(
                    key: const Key('bulk-approve'),
                    label: _selected!.isEmpty ? s.approve : '${s.approve} (${_selected!.length})',
                    icon: Icons.check_rounded,
                    onPressed: _selected!.isEmpty ? null : () => _decide(_selected!.toList(), Decision.approve),
                  ),
                ),
              ]),
            ),
    );
  }
}

class _Filters extends StatelessWidget {
  const _Filters({
    required this.scope,
    required this.search,
    required this.types,
    required this.type,
    required this.status,
    required this.onSearch,
    required this.onType,
    required this.onStatus,
  });

  final RequestScope scope;
  final TextEditingController search;
  final Map<String, int> types;
  final String? type;
  final ApprovalStatus? status;
  final VoidCallback onSearch;
  final ValueChanged<String?> onType;
  final ValueChanged<ApprovalStatus?> onStatus;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final statuses = scope == RequestScope.pending
        ? const <ApprovalStatus>[]
        : const [ApprovalStatus.pending, ApprovalStatus.approved, ApprovalStatus.rejected];
    return Container(
      color: p.surface,
      padding: const EdgeInsets.fromLTRB(0, IdaSpace.s3, 0, IdaSpace.s3),
      child: Column(children: [
        if (scope != RequestScope.mine)
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s4),
            child: TextField(
              controller: search,
              onChanged: (_) => onSearch(),
              textInputAction: TextInputAction.search,
              decoration: InputDecoration(
                hintText: s.searchRequests,
                prefixIcon: const Icon(Icons.search_rounded, size: IdaSizes.iconMd),
                isDense: true,
                suffixIcon: search.text.isEmpty
                    ? null
                    : IconButton(
                        tooltip: s.clearFilter,
                        icon: const Icon(Icons.close_rounded, size: IdaSizes.iconMd),
                        onPressed: () {
                          search.clear();
                          onSearch();
                        },
                      ),
              ),
            ),
          ),
        if (scope != RequestScope.mine) const SizedBox(height: IdaSpace.s3),
        SizedBox(
          height: IdaSizes.minTapTarget,
          child: ListView(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s4),
            children: [
              if (statuses.isNotEmpty) ...[
                for (final st in [null, ...statuses]) ...[
                  Center(
                    child: IdaFilterChip(
                      label: st == null ? s.all : s.status(st),
                      selected: status == st,
                      onTap: () => onStatus(st),
                    ),
                  ),
                  const SizedBox(width: IdaSpace.s2),
                ],
              ] else ...[
                Center(
                  child: IdaFilterChip(
                    label: '${s.filterType} · ${s.all}',
                    selected: type == null,
                    count: types.values.fold<int>(0, (a, b) => a + b),
                    onTap: () => onType(null),
                  ),
                ),
                const SizedBox(width: IdaSpace.s2),
                for (final e in types.entries) ...[
                  Center(
                    child: IdaFilterChip(
                      label: s.requestType(e.key),
                      selected: type == e.key,
                      count: e.value,
                      onTap: () => onType(type == e.key ? null : e.key),
                    ),
                  ),
                  const SizedBox(width: IdaSpace.s2),
                ],
              ],
            ],
          ),
        ),
      ]),
    );
  }
}

class _SelectAllBar extends StatelessWidget {
  const _SelectAllBar({required this.all, required this.some, required this.onChanged});
  final bool all;
  final bool some;
  final ValueChanged<bool> onChanged;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    return Material(
      color: p.accentSoft,
      child: InkWell(
        onTap: () => onChanged(!all),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s2),
          child: Row(children: [
            Checkbox(tristate: true, value: all ? true : (some ? null : false), onChanged: (_) => onChanged(!all)),
            Text(s.selectAll, style: context.text.labelLarge!.copyWith(color: p.accentStrong)),
          ]),
        ),
      ),
    );
  }
}
