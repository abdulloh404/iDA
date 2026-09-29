import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../app/app_controller.dart';
import '../../core/format.dart';
import '../../data/models.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import '../requests/request_detail_screen.dart';


const _maxFileKb = 5 * 1024;


class _Field {
  _Field(this.label, this.current, {this.keyboard, this.validate, this.formatters});
  final String label;
  final String current;
  final TextInputType? keyboard;
  final String? Function(S s, String v)? validate;
  final List<TextInputFormatter>? formatters;
  bool on = false;
  final controller = TextEditingController();
}


class EditProfileRequestScreen extends StatelessWidget {
  const EditProfileRequestScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    return AsyncView<DoctorProfile>(
      load: () => AppScope.read(context).repo.doctorProfile(),
      loading: Scaffold(appBar: AppBar(title: Text(s.editProfileTitle)), body: const SkeletonList()),
      builder: (context, d, _) => _EditProfileForm(profile: d),
    );
  }
}

class _EditProfileForm extends StatefulWidget {
  const _EditProfileForm({required this.profile});
  final DoctorProfile profile;

  @override
  State<_EditProfileForm> createState() => _EditProfileFormState();
}

class _EditProfileFormState extends State<_EditProfileForm> {
  late final List<_Field> _fields;
  final _reason = TextEditingController();
  final _files = <Attachment>[];
  final _errors = <String, String>{};
  String? _formError;
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    final d = widget.profile;
    final s = S(false);
    _fields = [
      _Field(s.phone, Fmt.maskPhone(d.phone),
          keyboard: TextInputType.phone,
          formatters: [FilteringTextInputFormatter.digitsOnly, LengthLimitingTextInputFormatter(10)],
          validate: (s, v) => RegExp(r'^0\d{9}$').hasMatch(v) ? null : s.invalidPhone),
      _Field(s.email, d.email,
          keyboard: TextInputType.emailAddress,
          validate: (s, v) => RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(v) ? null : s.invalidEmail),
      _Field(s.address, d.address, keyboard: TextInputType.streetAddress),
      _Field(s.nameEn, '${d.firstNameEn} ${d.lastNameEn}'),
      _Field(s.passportNo, d.passportNo ?? '-'),
    ];
  }

  @override
  void dispose() {
    for (final f in _fields) {
      f.controller.dispose();
    }
    _reason.dispose();
    super.dispose();
  }

  bool get _dirty => _fields.any((f) => f.on && f.controller.text.isNotEmpty) || _files.isNotEmpty || _reason.text.isNotEmpty;

  Future<void> _submit() async {
    final s = S.of(context);
    _errors.clear();
    _formError = null;
    final picked = _fields.where((f) => f.on).toList();
    if (picked.isEmpty) {
      setState(() => _formError = s.pickAtLeastOne);
      return;
    }
    for (final f in picked) {
      final v = f.controller.text.trim();
      if (v.isEmpty) {
        _errors[f.label] = s.required;
      } else if (v == f.current) {
        _errors[f.label] = s.sameAsCurrent;
      } else {
        final e = f.validate?.call(s, v);
        if (e != null) _errors[f.label] = e;
      }
    }
    if (_reason.text.trim().isEmpty) _errors['reason'] = s.required;
    if (_errors.isNotEmpty) {
      setState(() {});
      return;
    }
    setState(() => _busy = true);
    try {
      final req = await AppScope.read(context).repo.submitEditRequest(EditRequestDraft(
            requestType: RequestType.doctorProfile,
            changes: [for (final f in picked) FieldChange(f.label, f.current, f.controller.text.trim())],
            reason: _reason.text.trim(),
            attachments: List.of(_files),
          ));
      if (!mounted) return;
      Navigator.of(context).pushReplacement(MaterialPageRoute(
        builder: (_) => RequestSubmittedScreen(request: req, roleName: s.en ? 'Medical Director Office' : 'สำนักผู้อำนวยการแพทย์'),
      ));
    } catch (e) {
      if (mounted) setState(() => _formError = errorText(context, e));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    return _UnsavedGuard(
      dirty: _dirty && !_busy,
      child: Scaffold(
        appBar: AppBar(title: Text(s.editProfileTitle)),
        body: ListView(
          padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s4, IdaSpace.s4, IdaSpace.s8),
          children: [
            ContentWidth(
              child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                _Intro(text: s.editIntro),
                const SizedBox(height: IdaSpace.s4),
                for (final f in _fields) ...[
                  _FieldCard(
                    field: f,
                    error: _errors[f.label],
                    onToggle: (v) => setState(() {
                      f.on = v;
                      _formError = null;
                      _errors.remove(f.label);
                    }),
                    onChanged: () => setState(() => _errors.remove(f.label)),
                  ),
                  const SizedBox(height: IdaSpace.s3),
                ],
                const SizedBox(height: IdaSpace.s2),
                IdaCard(
                  child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                    IdaTextField(
                      fieldKey: const Key('edit-reason'),
                      label: s.reason,
                      hint: s.reasonHint,
                      controller: _reason,
                      maxLines: 3,
                      maxLength: 500,
                      error: _errors['reason'],
                      onChanged: (_) => setState(() => _errors.remove('reason')),
                    ),
                    const SizedBox(height: IdaSpace.s3),
                    _AttachmentPicker(files: _files, onChanged: () => setState(() {})),
                  ]),
                ),
                const SizedBox(height: IdaSpace.s3),
                _RoutePreview(approver: s.en ? 'Medical Director Office' : 'สำนักผู้อำนวยการแพทย์', cc: s.en ? 'MD office / Accounting' : 'สำนักแพทย์ / บัญชี'),
                if (_formError != null) ...[
                  const SizedBox(height: IdaSpace.s3),
                  _FormError(_formError!),
                ],
              ]),
            ),
          ],
        ),
        bottomNavigationBar: BottomActionBar(
          child: PrimaryButton(
            key: const Key('submit-edit'),
            label: s.submit,
            icon: Icons.send_rounded,
            loading: _busy,
            onPressed: _submit,
          ),
        ),
      ),
    );
  }
}

class _FieldCard extends StatelessWidget {
  const _FieldCard({required this.field, required this.onToggle, required this.onChanged, this.error});
  final _Field field;
  final ValueChanged<bool> onToggle;
  final VoidCallback onChanged;
  final String? error;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    return AnimatedContainer(
      duration: IdaMotion.of(context, IdaMotion.base),
      decoration: BoxDecoration(
        color: p.surface,
        borderRadius: IdaRadius.xlR,
        border: Border.all(color: field.on ? p.primaryText : p.border, width: field.on ? 1.5 : 1),
      ),
      child: Column(children: [
        InkWell(
          borderRadius: IdaRadius.xlR,
          onTap: () => onToggle(!field.on),
          child: Padding(
            padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s2, IdaSpace.s2, IdaSpace.s2),
            child: Row(children: [
              Expanded(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(field.label, style: context.text.titleSmall),
                  Text('${s.currentValue}: ${field.current}',
                      maxLines: 2, overflow: TextOverflow.ellipsis, style: context.text.bodySmall),
                ]),
              ),
              Checkbox(value: field.on, onChanged: (v) => onToggle(v ?? false)),
            ]),
          ),
        ),
        AnimatedSize(
          duration: IdaMotion.of(context, IdaMotion.base),
          child: field.on
              ? Padding(
                  padding: const EdgeInsets.fromLTRB(IdaSpace.s4, 0, IdaSpace.s4, IdaSpace.s4),
                  child: IdaTextField(
                    label: s.newValue,
                    controller: field.controller,
                    keyboardType: field.keyboard,
                    inputFormatters: field.formatters,
                    maxLines: field.keyboard == TextInputType.streetAddress ? 3 : 1,
                    error: error,
                    autofocus: true,
                    onChanged: (_) => onChanged(),
                  ),
                )
              : const SizedBox(width: double.infinity),
        ),
      ]),
    );
  }
}


class EditBankRequestScreen extends StatelessWidget {
  const EditBankRequestScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final repo = AppScope.read(context).repo;
    return AsyncView<(List<BankOption>, List<BankAccount>)>(
      load: () async {
        final r = await Future.wait([repo.banks(), repo.bankAccounts()]);
        return (r[0] as List<BankOption>, r[1] as List<BankAccount>);
      },
      loading: Scaffold(appBar: AppBar(title: Text(s.editBankTitle)), body: const SkeletonList()),
      builder: (context, data, _) => _EditBankForm(
        banks: data.$1,
        current: data.$2.firstWhere((a) => a.active, orElse: () => data.$2.first),
      ),
    );
  }
}

class _EditBankForm extends StatefulWidget {
  const _EditBankForm({required this.banks, required this.current});
  final List<BankOption> banks;
  final BankAccount current;

  @override
  State<_EditBankForm> createState() => _EditBankFormState();
}

class _EditBankFormState extends State<_EditBankForm> {
  BankOption? _bank;
  final _branch = TextEditingController();
  final _account = TextEditingController();
  late final _name = TextEditingController(text: widget.current.accountName);
  final _reason = TextEditingController();
  final _files = <Attachment>[];
  final _errors = <String, String>{};
  String? _formError;
  bool _busy = false;

  bool get _dirty => _bank != null || _branch.text.isNotEmpty || _account.text.isNotEmpty || _files.isNotEmpty;

  @override
  void dispose() {
    for (final c in [_branch, _account, _name, _reason]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _submit() async {
    final s = S.of(context);
    final th = S(false);
    _errors.clear();
    _formError = null;
    if (_bank == null) _errors['bank'] = s.required;
    if (_branch.text.trim().isEmpty) _errors['branch'] = s.required;
    final acc = _account.text.trim();
    if (!RegExp(r'^\d{10,12}$').hasMatch(acc)) _errors['account'] = s.invalidAccount;
    if (_name.text.trim().isEmpty) _errors['name'] = s.required;
    if (_files.isEmpty) _errors['files'] = s.bankBookRequired;
    if (_errors.isNotEmpty) {
      setState(() {});
      return;
    }
    final c = widget.current;
    setState(() => _busy = true);
    try {
      final req = await AppScope.read(context).repo.submitEditRequest(EditRequestDraft(
            requestType: RequestType.bankAccount,
            changes: [
              FieldChange(th.bank, '${c.bankNameTh} (${c.bankCode})', '${_bank!.nameTh} (${_bank!.code})'),
              FieldChange(th.branch, c.branchNameTh, _branch.text.trim()),
              FieldChange(th.accountNo, Fmt.maskAccount(c.accountNo), Fmt.maskAccount(acc)),
              if (_name.text.trim() != c.accountName) FieldChange(th.accountHolder, c.accountName, _name.text.trim()),
            ],
            reason: _reason.text.trim().isEmpty ? th.editBankTitle : _reason.text.trim(),
            attachments: List.of(_files),
          ));
      if (!mounted) return;
      Navigator.of(context).pushReplacement(MaterialPageRoute(
        builder: (_) => RequestSubmittedScreen(request: req, roleName: s.en ? 'Doctor Accounting' : 'บัญชีแพทย์'),
      ));
    } catch (e) {
      if (mounted) setState(() => _formError = errorText(context, e));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final c = widget.current;
    return _UnsavedGuard(
      dirty: _dirty && !_busy,
      child: Scaffold(
        appBar: AppBar(title: Text(s.editBankTitle)),
        body: ListView(
          padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s4, IdaSpace.s4, IdaSpace.s8),
          children: [
            ContentWidth(
              child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                _Intro(text: s.editIntro),
                const SizedBox(height: IdaSpace.s4),
                IdaCard(
                  child: Row(children: [
                    IconBox(Icons.account_balance_outlined, size: IdaSizes.avatarSm),
                    const SizedBox(width: IdaSpace.s3),
                    Expanded(
                      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        Text(s.currentValue, style: context.text.bodySmall),
                        Text('${c.bankNameTh} · ${Fmt.maskAccount(c.accountNo)}',
                            style: idaNumeric(context.text.bodyLarge!).copyWith(fontWeight: FontWeight.w500)),
                      ]),
                    ),
                  ]),
                ),
                const SizedBox(height: IdaSpace.s3),
                IdaCard(
                  child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                    IdaSelectField(
                      key: const Key('bank-select'),
                      label: s.newBank,
                      value: _bank == null ? null : '${_bank!.nameTh} (${_bank!.shortName})',
                      hint: s.chooseBank,
                      icon: Icons.account_balance_outlined,
                      error: _errors['bank'],
                      onTap: () async {
                        final b = await pickOption<BankOption>(
                          context,
                          title: s.chooseBank,
                          selected: _bank,
                          options: [for (final b in widget.banks) (b, '${b.nameTh} (${b.shortName})')],
                        );
                        if (b != null) {
                          setState(() {
                            _bank = b;
                            _errors.remove('bank');
                          });
                        }
                      },
                    ),
                    const SizedBox(height: IdaSpace.s4),
                    IdaTextField(
                      label: s.branch,
                      hint: s.branchHint,
                      controller: _branch,
                      error: _errors['branch'],
                      onChanged: (_) => setState(() => _errors.remove('branch')),
                    ),
                    const SizedBox(height: IdaSpace.s4),
                    IdaTextField(
                      fieldKey: const Key('bank-account'),
                      label: s.accountNo,
                      hint: s.accountNoHint,
                      controller: _account,
                      keyboardType: TextInputType.number,
                      inputFormatters: [FilteringTextInputFormatter.digitsOnly, LengthLimitingTextInputFormatter(12)],
                      error: _errors['account'],
                      onChanged: (_) => setState(() => _errors.remove('account')),
                    ),
                    const SizedBox(height: IdaSpace.s4),
                    IdaTextField(
                      label: s.accountHolder,
                      controller: _name,
                      error: _errors['name'],
                      onChanged: (_) => setState(() => _errors.remove('name')),
                    ),
                  ]),
                ),
                const SizedBox(height: IdaSpace.s3),
                IdaCard(
                  child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                    _AttachmentPicker(
                      files: _files,
                      required: true,
                      error: _errors['files'],
                      onChanged: () => setState(() => _errors.remove('files')),
                    ),
                    const SizedBox(height: IdaSpace.s3),
                    IdaTextField(label: s.reason, hint: s.reasonHint, controller: _reason, maxLines: 2, maxLength: 500),
                  ]),
                ),
                const SizedBox(height: IdaSpace.s3),
                _RoutePreview(approver: s.en ? 'Doctor Accounting' : 'บัญชีแพทย์', cc: s.en ? 'MD office' : 'สำนักแพทย์'),
                if (_formError != null) ...[const SizedBox(height: IdaSpace.s3), _FormError(_formError!)],
              ]),
            ),
          ],
        ),
        bottomNavigationBar: BottomActionBar(
          child: PrimaryButton(
            key: const Key('submit-bank'),
            label: s.submit,
            icon: Icons.send_rounded,
            loading: _busy,
            onPressed: _submit,
          ),
        ),
      ),
    );
  }
}


class _Intro extends StatelessWidget {
  const _Intro({required this.text});
  final String text;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    return Container(
      padding: const EdgeInsets.all(IdaSpace.s3),
      decoration: BoxDecoration(color: p.accentSoft, borderRadius: IdaRadius.mdR),
      child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Icon(Icons.info_outline_rounded, size: IdaSizes.iconMd, color: p.accentStrong),
        const SizedBox(width: IdaSpace.s2),
        Expanded(child: Text(text, style: context.text.bodyMedium!.copyWith(color: p.accentStrong))),
      ]),
    );
  }
}

class _FormError extends StatelessWidget {
  const _FormError(this.message);
  final String message;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    return Semantics(
      liveRegion: true,
      child: Container(
        padding: const EdgeInsets.all(IdaSpace.s3),
        decoration: BoxDecoration(color: p.dangerSoft, borderRadius: IdaRadius.mdR),
        child: Row(children: [
          Icon(Icons.error_outline_rounded, size: IdaSizes.iconMd, color: p.dangerText),
          const SizedBox(width: IdaSpace.s2),
          Expanded(child: Text(message, style: context.text.bodyMedium!.copyWith(color: p.dangerText))),
        ]),
      ),
    );
  }
}


class _RoutePreview extends StatelessWidget {
  const _RoutePreview({required this.approver, required this.cc});
  final String approver;
  final String cc;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    Widget step(IconData icon, String text, {bool last = false}) => Row(children: [
          Column(children: [
            IconBox(icon, size: IdaSizes.avatarSm - IdaSpace.s1),
            if (!last) Container(width: 2, height: IdaSpace.s3, color: p.border),
          ]),
          const SizedBox(width: IdaSpace.s3),
          Expanded(
            child: Padding(
              padding: EdgeInsets.only(bottom: last ? 0 : IdaSpace.s3),
              child: Text(text, style: context.text.bodyMedium),
            ),
          ),
        ]);
    return IdaCard(
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Text(s.approvalRoute, style: context.text.titleSmall),
        const SizedBox(height: IdaSpace.s3),
        step(Icons.send_rounded, s.routeYou),
        step(Icons.how_to_reg_outlined, approver),
        step(Icons.mail_outline_rounded, s.routeNotify(cc), last: true),
      ]),
    );
  }
}

class _AttachmentPicker extends StatelessWidget {
  const _AttachmentPicker({required this.files, required this.onChanged, this.required = false, this.error});
  final List<Attachment> files;
  final VoidCallback onChanged;
  final bool required;
  final String? error;

  Future<void> _pick(BuildContext context) async {
    try {
      final picked = await FilePicker.pickFiles(type: FileType.custom, allowedExtensions: const ['pdf', 'jpg', 'jpeg', 'png']);
      for (final f in picked) {
        final kb = (await f.xFile.length()) ~/ 1024;
        if (kb > _maxFileKb) {
          if (context.mounted) showToast(context, S.of(context).attachHint, tone: ToastTone.error);
          continue;
        }
        files.add(Attachment(f.name, kb));
      }
      onChanged();
    } catch (e) {
      if (context.mounted) showErrorToast(context, e);
    }
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      Row(children: [
        Expanded(child: Text('${s.attachments}${required ? ' *' : ''}', style: context.text.labelLarge!.copyWith(color: p.textSecondary))),
      ]),
      const SizedBox(height: IdaSpace.s1),
      Text(s.attachHint, style: context.text.bodySmall),
      const SizedBox(height: IdaSpace.s2),
      for (final (i, f) in files.indexed)
        FileRow(
          name: f.name,
          detail: Fmt.fileSize(f.sizeKb),
          onRemove: () {
            files.removeAt(i);
            onChanged();
          },
        ),
      OutlinedButton.icon(
        style: OutlinedButton.styleFrom(side: BorderSide(color: error != null ? p.danger : p.primaryText, width: 1.5)),
        onPressed: () => _pick(context),
        icon: const Icon(Icons.attach_file_rounded, size: IdaSizes.iconMd),
        label: Text(s.addAttachment),
      ),
      if (error != null) ...[
        const SizedBox(height: IdaSpace.s1),
        Row(children: [
          Icon(Icons.error_outline_rounded, size: IdaSizes.iconXs, color: p.dangerText),
          const SizedBox(width: IdaSpace.s1),
          Text(error!, style: context.text.bodySmall!.copyWith(color: p.dangerText)),
        ]),
      ],
    ]);
  }
}


class _UnsavedGuard extends StatelessWidget {
  const _UnsavedGuard({required this.dirty, required this.child});
  final bool dirty;
  final Widget child;

  @override
  Widget build(BuildContext context) => PopScope(
        canPop: !dirty,
        onPopInvokedWithResult: (didPop, _) async {
          if (didPop) return;
          final s = S.of(context);
          final leave = await confirmDialog(
            context,
            title: s.unsavedTitle,
            body: s.unsavedBody,
            confirmLabel: s.discard,
            destructive: true,
            icon: Icons.edit_off_rounded,
          );
          if (leave && context.mounted) Navigator.of(context).pop();
        },
        child: child,
      );
}


class RequestSubmittedScreen extends StatelessWidget {
  const RequestSubmittedScreen({super.key, required this.request, required this.roleName});
  final ApprovalRequest request;
  final String roleName;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    return Scaffold(
      appBar: AppBar(title: Text(s.requestSent), automaticallyImplyLeading: false),
      body: SafeArea(
        child: ContentWidth(
          child: Padding(
            padding: const EdgeInsets.all(IdaSpace.s6),
            child: Column(children: [
              const Spacer(),
              IconBox(Icons.check_rounded, size: IdaSizes.avatarLg + IdaSpace.s4, bg: p.successSoft, fg: p.success),
              const SizedBox(height: IdaSpace.s5),
              Text(s.requestSent, style: context.text.headlineSmall, textAlign: TextAlign.center),
              const SizedBox(height: IdaSpace.s2),
              Text(s.requestSentBody(request.requestNo, roleName),
                  textAlign: TextAlign.center, style: context.text.bodyLarge!.copyWith(color: p.textSecondary)),
              const Spacer(),
              PrimaryButton(
                label: s.viewRequest,
                icon: Icons.visibility_outlined,
                onPressed: () => Navigator.of(context).pushReplacement(
                  MaterialPageRoute(builder: (_) => RequestDetailScreen(id: request.id)),
                ),
              ),
              const SizedBox(height: IdaSpace.s2),
              TextButton(
                onPressed: () => Navigator.of(context).popUntil((r) => r.isFirst),
                child: Text(s.backHome),
              ),
            ]),
          ),
        ),
      ),
    );
  }
}
