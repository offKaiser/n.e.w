# RELATÓRIO TÉCNICO — PROJECT N.E.W.

Atualização: 06/09/2026

---

## 1. Objetivo do projeto

O **Project N.E.W.** é um protótipo técnico de MOBA 3D. O objetivo atual é manter uma vertical slice jogável, com duas campeãs, combate, IA, minions, torres, progressão e uma base funcional de multiplayer LAN antes de avançar para conteúdo e arte definitiva.

Fluxo técnico realizado:

```text
Movimento
→ Controle por clique e câmera
→ Combate e habilidades
→ IA, minions e torres
→ Mapa e progressão
→ Multiplayer LAN
→ Arte e polimento inicial
```

---

## 2. Stack

```text
Engine:    Godot 4.7.2 .NET
Linguagem: C#
SDK:       .NET
IDE:       Visual Studio Code
```

Comando de compilação:

```powershell
dotnet build
```

Status confirmado no estado atual:

```text
BUILD C# → 0 erros / 0 avisos
```

---

## 3. Localização e repositório

Projeto local:

```text
C:\Users\marti\OneDrive\Documentos\Projects\ProjetctNew\projeto-n.e.w
```

Repositório GitHub:

```text
https://github.com/offKaiser/n.e.w
```

Branch principal publicada:

```text
main
```

Commit inicial publicado:

```text
d225572 — Initial commit: Project N.E.W prototype
```

---

## 4. Estrutura principal

```text
Assets/
├── Textures/
│   ├── Champions/
│   │   ├── nyr_vela_concept.png
│   │   └── nyxara_concept.png
│   └── Map/
│       └── four_biomes_terrain.png
│
Scenes/
├── Game/Main.tscn
└── Units/
    ├── Minion.tscn
    └── Tower.tscn
│
Scripts/
├── Abilities/
├── AI/
├── Combat/
├── Core/
├── Heroes/
├── Systems/
├── UI/
└── VFX/
```

---

## 5. Sistemas implementados

### Movimento e câmera

```text
✓ Movimento por clique no terreno
✓ Seleção de alvo por clique
✓ Movimento de desenvolvimento por I/J/K/L
✓ Rotação automática na direção de movimento/alvo
✓ Câmera isométrica com smooth follow
✓ Marcador de destino
```

### Combate e recursos

```text
✓ Vida, mana e barras no HUD
✓ Ataque básico, alcance e cooldown
✓ Morte, recompensa e condição de vitória/derrota
✓ Ouro e experiência por minions, heróis e torres
✓ Níveis até 16 e pontos de habilidade
✓ Evolução de Q/W/E/R por 1/2/3/4
```

### Minions, torres e lane

```text
✓ Ondas automáticas a cada 12 segundos
✓ Composição: 3 melee + 1 tank + 3 ranged
✓ Minions melee, tank e ranged com atributos distintos
✓ Projéteis visíveis para minions ranged
✓ Torre azul e torre vermelha com aquisição de alvo por time
✓ Colisão ajustada para minions não ficarem presos nas torres
✓ Lane central funcional entre as bases
```

---

## 6. Campeãs atuais

### Nyr'Vela — maga de controle

```text
Q — Orbe Abissal
    Explosão em área no alvo, dano e acumulação da passiva.

W — Correntes do Vazio
    Dano, slow por 2,5 s e visual de corrente.

E — Passo Sombrio
    Dash curto que deixa uma sombra; ela explode após 1,5 s.

R — Domínio Abissal
    Campo de 5 s com dano por pulso, slow, redução de cooldown da inimiga,
    redução de dano recebida para Nyr'Vela e geração acelerada de Energia Abissal.

Passiva — Eco do Abismo
    Habilidades acumulam Energia Abissal. Em 100, o próximo ataque ou
    habilidade recebe dano extra e marca o alvo por 3 s. Uma eliminação
    marcada reduz as recargas. Eliminações no Domínio estendem seu tempo.
```

### Nyxara — atiradora controlada por IA

```text
Passiva — Precisão Sombria
    A cada três ataques, causa dano fortalecido.

Q — Granada Sombria
    Dano em área e slow.

W — Fúria Celeste
    Buff temporário de velocidade de ataque.

E — Passo Sombrio
    Dash de reposicionamento.

R — Canhão Estelar
    Raio longo perfurante, com telegráfico visual.
```

### Apresentação visual

```text
✓ Artes próprias aplicadas a Nyr'Vela e Nyxara em Sprite3D.
✓ Escala padronizada entre as duas campeãs.
✓ Animação simples de caminhada: balanço alternado das pernas e bob do corpo.
✓ VFX para impactos, área, correntes, dash, campo e canhão.
```

---

## 7. Mapa atual

O mapa usa uma textura única com quatro biomas:

```text
Noroeste → neve
Nordeste → deserto
Sudoeste → floresta
Sudeste → abismo
Centro   → lane central de pedra
```

Estado visual:

```text
✓ Textura de terreno aplicada em um plano 100 x 100.
✓ Lane central integrada à própria textura, sem faixa plana sobreposta.
✓ Iluminação ambiente, glow e sombras configurados.
✓ Bases azul/vermelha e torres posicionadas na lane.
✓ Torres usam visual de obelisco com núcleo da cor da equipe.
✓ Antigos objetos de teste e marcadores coloridos estão desativados.
```

---

## 8. Multiplayer LAN

Ativação dentro do jogo:

```text
F1 → hospedar na porta 7000
F2 → entrar em 127.0.0.1
```

Outro computador:

```text
-- --join=IP_DO_HOST
```

Estado sincronizado pelo host:

```text
✓ Jogadores e transformações
✓ Minions: spawn, identidade, movimento e vida
✓ Nyxara/IA: posição e VFX
✓ Vida, mana, ouro, XP, níveis e pontos
✓ Ranks e cooldowns das habilidades
✓ VFX de habilidade relevantes
```

Validações realizadas:

```text
✓ Cena carregada em Godot headless
✓ Assets importados pelo Godot
✓ Host e cliente LAN conectaram localmente
✓ Correção aplicada para impedir RPCs após o encerramento da sessão
```

---

## 9. Controles

| Ação | Controle |
| --- | --- |
| Mover por clique | Botão esquerdo no terreno |
| Atacar/selecionar | Botão esquerdo em unidade inimiga |
| Movimento de desenvolvimento | I / J / K / L |
| Habilidades | Q / W / E / R |
| Evoluir habilidades | 1 / 2 / 3 / 4 |
| Hospedar LAN | F1 |
| Entrar em localhost | F2 |
| Reiniciar partida | Enter |

---

## 10. Pendências reais

O projeto deixou de ser apenas um protótipo técnico básico, mas ainda não é um MOBA completo. As próximas evoluções recomendadas são:

```text
1. Validar visualmente em uma sessão longa as alterações recentes de mapa e caminhada.
2. Refinar a arte: modelos/rigs 3D finais, animações reais, sons e partículas finais.
3. Criar seleção de campeões e tornar Nyxara jogável em multiplayer.
4. Adicionar o terceiro campeão, a partir de referência visual e design de habilidades.
5. Evoluir o mapa para múltiplas lanes, jungle, objetivos neutros e NavMesh.
6. Adicionar lobby, seleção de time, reconexão e validações de rede mais robustas.
7. Separar atributos e balanceamento em recursos de dados (`Resource`/JSON).
```

---

## 11. Como continuar com outro agente/chat

Use este texto como contexto inicial:

> Continue o Project N.E.W. a partir do estado atual. Preserve a estrutura Godot/C# e os sistemas existentes. Compile com `dotnet build` após alterações relevantes. Antes de criar recursos novos, valide a cena `Scenes/Game/Main.tscn`. O próximo foco é polimento visual do mapa/campeões ou o terceiro campeão, conforme a referência fornecida.

