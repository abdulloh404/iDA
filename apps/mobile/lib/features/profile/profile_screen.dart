import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../core/format.dart';
import '../../data/models.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import '../requests/request_detail_screen.dart' show FileRow;
import 'edit_request_screen.dart';


class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

enum _Tab { personal, other, documents }

class _ProfileScreenState extends State<ProfileScreen> {
  _Tab _tab = _Tab.personal;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(s.menuProfile)),
      body: AsyncView<DoctorProfile>(
        load: () => AppScope.read(context).repo.doctorProfile(),
        loading: const SkeletonList(header: true),
        builder: (context, d, reload) => RefreshIndicator(
          onRefresh: reload,
          child: ListView(
            padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s4, IdaSpace.s4, IdaSpace.s8),
            children: [
              ContentWidth(
                child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                  _ProfileHeader(d: d),
                  const SizedBox(height: IdaSpace.s4),
                  IdaSegmented<_Tab>(
                    options: {_Tab.personal: s.tabPersonal, _Tab.other: s.tabOther, _Tab.documents: s.tabDocuments},
                    value: _tab,
                    onChanged: (t) => setState(() => _tab = t),
                  ),
                  const SizedBox(height: IdaSpace.s4),
                  ...switch (_tab) {
                    _Tab.personal => _personal(context, d),
                    _Tab.other => _other(context, d),
                    _Tab.documents => _documents(context, d),
                  },
                ]),
              ),
            ],
          ),
        ),
      ),
    );
  }

  List<Widget> _personal(BuildContext context, DoctorProfile d) {
    final s = S.of(context);
    final en = s.en;
    final licenseSoon = d.licenseExpiry.difference(DateTime.now()).inDays < 90;
    return [
      ExpandableSection(title: s.personalInfo, icon: Icons.person_outline_rounded, children: [
        Row(children: [
          Expanded(child: KeyValue(s.prefixTh, d.prefixTh)),
          Expanded(child: KeyValue(s.prefixEn, d.prefixEn)),
        ]),
        KeyValue(s.nameTh, '${d.firstNameTh} ${d.lastNameTh}'),
        KeyValue(s.nameEn, '${d.firstNameEn} ${d.lastNameEn}'),
        KeyValue(s.nationalId, null,
            valueWidget: RevealText(masked: Fmt.maskNationalId(d.nationalId), full: Fmt.formatNationalId(d.nationalId))),
        KeyValue(s.birthDate, Fmt.dateLong(d.birthDate, en)),
        Row(children: [
          Expanded(child: KeyValue(s.passportNo, d.passportNo, numeric: true)),
          Expanded(child: KeyValue(s.passportExpiry, d.passportExpiry == null ? null : Fmt.date(d.passportExpiry!, en))),
        ]),
      ]),
      const SizedBox(height: IdaSpace.s3),
      ExpandableSection(title: s.contactInfo, icon: Icons.contact_phone_outlined, children: [
        KeyValue(s.phone, Fmt.maskPhone(d.phone), numeric: true),
        KeyValue(s.email, d.email),
        KeyValue(s.address, d.address),
      ]),
      const SizedBox(height: IdaSpace.s3),
      ExpandableSection(title: s.professional, icon: Icons.workspace_premium_outlined, children: [
        KeyValue(s.licenseNo, d.licenseNo, numeric: true),
        KeyValue(
          s.licenseExpiry,
          null,
          valueWidget: Wrap(spacing: IdaSpace.s2, crossAxisAlignment: WrapCrossAlignment.center, children: [
            Text(Fmt.dateLong(d.licenseExpiry, en), style: context.text.bodyLarge!.copyWith(fontWeight: FontWeight.w500)),
            if (licenseSoon)
              IdaBadge(label: s.expiresOn(Fmt.date(d.licenseExpiry, en)), tone: IdaBadgeTone.pending, icon: Icons.warning_amber_rounded),
          ]),
        ),
        KeyValue(s.specialty, d.specialty),
        KeyValue(s.subSpecialty, d.subSpecialty),
        KeyValue(s.department, d.department),
      ]),
    ];
  }

  List<Widget> _other(BuildContext context, DoctorProfile d) {
    final s = S.of(context);
    final p = context.ida;
    return [
      ExpandableSection(title: s.educationTitle, icon: Icons.school_outlined, children: [
        for (final e in d.education)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: IdaSpace.s2),
            child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Container(
                padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s2, vertical: 2),
                decoration: BoxDecoration(color: p.primarySoft, borderRadius: IdaRadius.smR),
                child: Text('${Fmt.year(e.year, s.en)}', style: idaNumeric(context.text.labelMedium!).copyWith(color: p.primaryText)),
              ),
              const SizedBox(width: IdaSpace.s3),
              Expanded(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(e.degree, style: context.text.bodyLarge!.copyWith(fontWeight: FontWeight.w500)),
                  Text(e.institute, style: context.text.bodySmall),
                ]),
              ),
            ]),
          ),
      ]),
      const SizedBox(height: IdaSpace.s3),
      ExpandableSection(title: s.doctorCodes, icon: Icons.qr_code_2_rounded, children: [
        for (final c in d.codes)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: IdaSpace.s2),
            child: Row(children: [
              Expanded(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(c.hospitalNameTh, style: context.text.bodyLarge!.copyWith(fontWeight: FontWeight.w500)),
                  Text('${c.code} · ${c.doctorType}', style: idaNumeric(context.text.bodySmall!)),
                ]),
              ),
              c.active
                  ? IdaBadge(label: s.active, tone: IdaBadgeTone.success, icon: Icons.check_circle_rounded)
                  : IdaBadge(label: s.inactive, tone: IdaBadgeTone.neutral, icon: Icons.remove_circle_outline_rounded),
            ]),
          ),
      ]),
    ];
  }

  List<Widget> _documents(BuildContext context, DoctorProfile d) {
    final s = S.of(context);
    return [
      IdaCard(
        padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s3, IdaSpace.s2, IdaSpace.s3),
        child: Column(children: [
          for (final doc in d.documents)
            Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              FileRow(
                name: doc.nameTh,
                detail: '${doc.fileName} · ${Fmt.fileSize(doc.sizeKb)} · ${s.uploadedOn(Fmt.date(doc.uploadedAt, s.en))}',
              ),
              if (doc.expiresAt != null)
                Padding(
                  padding: const EdgeInsets.only(left: IdaSizes.avatarSm + IdaSpace.s3, bottom: IdaSpace.s2),
                  child: IdaBadge(label: s.expiresOn(Fmt.date(doc.expiresAt!, s.en)), tone: IdaBadgeTone.info, icon: Icons.event_outlined),
                ),
            ]),
        ]),
      ),
    ];
  }
}

class _ProfileHeader extends StatelessWidget {
  const _ProfileHeader({required this.d});
  final DoctorProfile d;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final session = AppScope.of(context).requireSession;
    return IdaCard(
      child: Column(children: [
        Row(children: [
          IdaAvatar(session.user.initials, size: IdaSizes.avatarLg),
          const SizedBox(width: IdaSpace.s4),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(d.fullNameTh, style: context.text.titleMedium),
              Text(d.fullNameEn, style: context.text.bodyMedium!.copyWith(color: p.textSecondary)),
              const SizedBox(height: IdaSpace.s1),
              Text(d.subSpecialty, style: context.text.bodySmall),
              const SizedBox(height: IdaSpace.s2),
              Wrap(spacing: IdaSpace.s2, runSpacing: IdaSpace.s1, children: [
                IdaBadge(label: session.hospital.doctorCode ?? '-', tone: IdaBadgeTone.closed, icon: Icons.badge_outlined),
                IdaBadge(label: s.active, tone: IdaBadgeTone.success, icon: Icons.check_circle_rounded),
              ]),
            ]),
          ),
        ]),
        const SizedBox(height: IdaSpace.s4),
        SizedBox(
          width: double.infinity,
          child: OutlinedButton.icon(
            onPressed: () => pushPage<void>(context, const EditProfileRequestScreen()),
            icon: const Icon(Icons.edit_note_rounded, size: IdaSizes.iconMd),
            label: Text(s.requestEdit),
          ),
        ),
      ]),
    );
  }
}


class RevealText extends StatefulWidget {
  const RevealText({super.key, required this.masked, required this.full});
  final String masked;
  final String full;

  @override
  State<RevealText> createState() => _RevealTextState();
}

class _RevealTextState extends State<RevealText> {
  bool _show = false;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    return Row(children: [
      Expanded(
        child: Text(
          _show ? widget.full : widget.masked,
          style: idaNumeric(context.text.bodyLarge!).copyWith(fontWeight: FontWeight.w500),
        ),
      ),
      IconButton(
        tooltip: _show ? s.hide : s.show,
        onPressed: () => setState(() => _show = !_show),
        icon: Icon(_show ? Icons.visibility_off_outlined : Icons.visibility_outlined, size: IdaSizes.iconMd),
      ),
    ]);
  }
}
