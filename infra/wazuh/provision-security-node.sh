#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# CosTamarindo 2D - aprovisionamiento del nodo Sg-Security
#
# Se ejecuta como root en una VM Multipass nueva (Ubuntu 24.04 LTS):
#   sudo ALLOWED_SUBNET=172.30.224.0/20 bash provision-security-node.sh
#
# Deja:
#   - hostname sg-security
#   - SSH solo con clave, sin root
#   - UFW con 22/443/1514/1515 solo desde la subred indicada (Default Switch)
#   - Wazuh all-in-one 4.14.x (manager + indexer + dashboard)
#   - authd con contrasena de registro generada en la propia VM
#   - reglas 100200-100250 y grupo `game` desde /tmp/costamarindo/
#
# No contiene secretos: la contrasena de authd se genera en la VM y queda en
# /root/wazuh-registration-password.txt (modo 600). El instalador guarda las
# credenciales del dashboard en /root/wazuh-install-files.tar.
# ---------------------------------------------------------------------------
set -euo pipefail

ALLOWED_SUBNET="${ALLOWED_SUBNET:-172.30.224.0/20}"
WAZUH_MINOR="${WAZUH_MINOR:-4.14}"
NODE_HOSTNAME="${NODE_HOSTNAME:-sg-security}"
RULES_SRC="${RULES_SRC:-/tmp/costamarindo/costamarindo_rules.xml}"
GROUP_SRC="${GROUP_SRC:-/tmp/costamarindo/game.conf}"

export DEBIAN_FRONTEND=noninteractive

log() { printf '\n[%s] %s\n' "$(date -u +%H:%M:%S)" "$*"; }

log "Hostname: ${NODE_HOSTNAME}"
hostnamectl set-hostname "${NODE_HOSTNAME}"

log "Actualizando paquetes base"
apt-get update
apt-get -y upgrade
apt-get install -y --no-install-recommends \
  ca-certificates curl gnupg jq lsb-release openssh-server openssl ufw

log "Endureciendo SSH (solo clave, sin root)"
install -d -m 0755 /etc/ssh/sshd_config.d
cat >/etc/ssh/sshd_config.d/99-costamarindo.conf <<'EOF'
PasswordAuthentication no
KbdInteractiveAuthentication no
PermitRootLogin no
PubkeyAuthentication yes
EOF
systemctl reload ssh

log "Configurando UFW (permitir SSH antes de habilitar)"
ufw --force reset
ufw default deny incoming
ufw default allow outgoing
ufw allow from "${ALLOWED_SUBNET}" to any port 22 proto tcp comment 'multipass ssh'
ufw allow from "${ALLOWED_SUBNET}" to any port 1514 proto tcp comment 'wazuh agent'
ufw allow from "${ALLOWED_SUBNET}" to any port 1515 proto tcp comment 'wazuh authd'
ufw allow from "${ALLOWED_SUBNET}" to any port 443 proto tcp comment 'wazuh dashboard'
ufw --force enable
ufw status verbose

if [ -x /var/ossec/bin/wazuh-control ]; then
  log "Wazuh ya esta instalado; se omite el instalador all-in-one"
else
  log "Instalando Wazuh all-in-one ${WAZUH_MINOR}.x"
  cd /root
  if [ ! -s wazuh-install.sh ]; then
    curl -fsSLO "https://packages.wazuh.com/${WAZUH_MINOR}/wazuh-install.sh"
  fi
  # -a: all-in-one; -i: ignorar comprobacion de hardware (VM de laboratorio)
  bash wazuh-install.sh -a -i
fi

log "Esperando al manager"
for _ in $(seq 1 90); do
  if /var/ossec/bin/wazuh-control status >/dev/null 2>&1; then
    break
  fi
  sleep 2
done

log "Generando contrasena de registro authd (si no existe)"
if [ ! -s /var/ossec/etc/authd.pass ]; then
  umask 077
  openssl rand -base64 36 | tr -d '\n' > /var/ossec/etc/authd.pass
fi
chmod 600 /var/ossec/etc/authd.pass
chown root:wazuh /var/ossec/etc/authd.pass 2>/dev/null || true
cp /var/ossec/etc/authd.pass /root/wazuh-registration-password.txt
chmod 600 /root/wazuh-registration-password.txt

log "Habilitando authd con contrasena"
python3 - <<'PY'
import re
p = '/var/ossec/etc/ossec.conf'
s = open(p, encoding='utf-8').read()
if '<auth>' in s:
    if '<use_password>' in s:
        s = re.sub(r'<use_password>.*?</use_password>', '<use_password>yes</use_password>', s, flags=re.S)
    else:
        s = s.replace('</auth>', '  <use_password>yes</use_password>\n  </auth>')
else:
    s = s.replace('</ossec_config>', '''  <auth>
    <disabled>no</disabled>
    <port>1515</port>
    <use_password>yes</use_password>
  </auth>
</ossec_config>''')
open(p, 'w', encoding='utf-8').write(s)
PY

log "Restringiendo la API de Wazuh a localhost"
API_YAML=/var/ossec/api/configuration/api.yaml
if [ -f "${API_YAML}" ]; then
  if grep -q '^# host:' "${API_YAML}"; then
    sed -i "s|^# host:.*|host: ['127.0.0.1']|" "${API_YAML}"
  elif grep -q '^host:' "${API_YAML}"; then
    sed -i "s|^host:.*|host: ['127.0.0.1']|" "${API_YAML}"
  else
    sed -i "1i host: ['127.0.0.1']" "${API_YAML}"
  fi
fi

log "Instalando reglas y grupo game"
if [ -f "${RULES_SRC}" ]; then
  install -o wazuh -g wazuh -m 0640 "${RULES_SRC}" /var/ossec/etc/rules/costamarindo_rules.xml
else
  log "AVISO: no existe ${RULES_SRC}; se omite la regla"
fi
if [ -f "${GROUP_SRC}" ]; then
  install -d -o wazuh -g wazuh -m 0750 /var/ossec/etc/shared/game
  install -o wazuh -g wazuh -m 0640 "${GROUP_SRC}" /var/ossec/etc/shared/game/agent.conf
  chown -R wazuh:wazuh /var/ossec/etc/shared/game
else
  log "AVISO: no existe ${GROUP_SRC}; se omite el grupo"
fi

log "Reiniciando Wazuh"
/var/ossec/bin/wazuh-control restart
sleep 8
/var/ossec/bin/wazuh-control status || true

log "Puertos en escucha"
ss -tlnp | grep -E ':(22|443|1514|1515|55000|9200)\b' || true

log "Version instalada"
if [ -x /var/ossec/bin/wazuh-control ]; then
  /var/ossec/bin/wazuh-control info || true
fi

log "Listo. Credenciales del dashboard en /root/wazuh-install-files.tar"
log "Si no necesitás depurar, eliminá /var/log/costamarindo-provision.log y /var/log/wazuh-install.log: pueden contener credenciales generadas."
echo "PROVISION_OK"
