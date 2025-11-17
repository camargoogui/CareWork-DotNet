using Microsoft.EntityFrameworkCore;
using CareWork.Infrastructure.Models;

namespace CareWork.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedTipsAsync(CareWorkDbContext context)
    {
        // Verificar se já existem tips no banco
        if (await context.Tips.AnyAsync())
        {
            return; // Já foi populado
        }

        var tips = new List<Tip>
        {
            // Tips de Stress
            new Tip
            {
                Id = Guid.NewGuid(),
                Title = "Técnicas de Respiração Profunda",
                Description = "Pratique respiração profunda por 5 minutos: inspire por 4 segundos, segure por 4, expire por 6. Isso ajuda a reduzir o stress imediatamente e acalmar o sistema nervoso.",
                Icon = "breath",
                Color = "#FF5722",
                Category = "Stress",
                CreatedAt = DateTime.UtcNow
            },
            new Tip
            {
                Id = Guid.NewGuid(),
                Title = "Meditação Matinal",
                Description = "Comece o dia com 10 minutos de meditação. Use apps como Headspace ou apenas sente-se em silêncio focando na respiração. Isso reduz o cortisol e melhora o foco.",
                Icon = "meditation",
                Color = "#9C27B0",
                Category = "Stress",
                CreatedAt = DateTime.UtcNow
            },
            new Tip
            {
                Id = Guid.NewGuid(),
                Title = "Técnica 5-4-3-2-1",
                Description = "Quando estiver ansioso, identifique: 5 coisas que você vê, 4 que pode tocar, 3 que pode ouvir, 2 que pode cheirar, 1 que pode saborear. Isso ajuda a se ancorar no presente.",
                Icon = "focus",
                Color = "#795548",
                Category = "Stress",
                CreatedAt = DateTime.UtcNow
            },
            new Tip
            {
                Id = Guid.NewGuid(),
                Title = "Pausas Ativas",
                Description = "A cada 2 horas de trabalho, faça uma pausa de 5 minutos. Caminhe, alongue-se ou apenas respire. Isso previne o acúmulo de stress.",
                Icon = "pause",
                Color = "#F44336",
                Category = "Stress",
                CreatedAt = DateTime.UtcNow
            },
            new Tip
            {
                Id = Guid.NewGuid(),
                Title = "Limite de Notícias",
                Description = "Evite consumir notícias negativas antes de dormir ou logo ao acordar. Defina horários específicos para se informar e limite o tempo.",
                Icon = "news-off",
                Color = "#E91E63",
                Category = "Stress",
                CreatedAt = DateTime.UtcNow
            },

            // Tips de Sleep
            new Tip
            {
                Id = Guid.NewGuid(),
                Title = "Rotina de Sono Consistente",
                Description = "Mantenha um horário regular para dormir e acordar, mesmo nos fins de semana. Isso ajuda a regular seu relógio biológico e melhora a qualidade do sono.",
                Icon = "moon",
                Color = "#2196F3",
                Category = "Sleep",
                CreatedAt = DateTime.UtcNow
            },
            new Tip
            {
                Id = Guid.NewGuid(),
                Title = "Evite Telas Antes de Dormir",
                Description = "Desligue celulares, tablets e TVs pelo menos 1 hora antes de dormir. A luz azul interfere na produção de melatonina, hormônio do sono.",
                Icon = "phone-off",
                Color = "#607D8B",
                Category = "Sleep",
                CreatedAt = DateTime.UtcNow
            },
            new Tip
            {
                Id = Guid.NewGuid(),
                Title = "Ambiente Escuro e Fresco",
                Description = "Mantenha o quarto escuro (use cortinas blackout) e com temperatura entre 18-22°C. Um ambiente adequado é essencial para um sono reparador.",
                Icon = "bedroom",
                Color = "#3F51B5",
                Category = "Sleep",
                CreatedAt = DateTime.UtcNow
            },
            new Tip
            {
                Id = Guid.NewGuid(),
                Title = "Evite Cafeína à Tarde",
                Description = "Evite café, chá preto e energéticos após as 14h. A cafeína pode permanecer no organismo por até 8 horas, afetando o sono noturno.",
                Icon = "coffee-off",
                Color = "#009688",
                Category = "Sleep",
                CreatedAt = DateTime.UtcNow
            },
            new Tip
            {
                Id = Guid.NewGuid(),
                Title = "Ritual Relaxante",
                Description = "Crie um ritual antes de dormir: leia um livro, tome um banho morno, ouça música calma. Isso sinaliza ao cérebro que é hora de descansar.",
                Icon = "spa",
                Color = "#00BCD4",
                Category = "Sleep",
                CreatedAt = DateTime.UtcNow
            },

            // Tips de Mood
            new Tip
            {
                Id = Guid.NewGuid(),
                Title = "Gratidão Diária",
                Description = "Escreva 3 coisas pelas quais você é grato todos os dias. Isso melhora o humor, reduz ansiedade e aumenta a sensação de bem-estar.",
                Icon = "heart",
                Color = "#E91E63",
                Category = "Mood",
                CreatedAt = DateTime.UtcNow
            },
            new Tip
            {
                Id = Guid.NewGuid(),
                Title = "Tempo ao Ar Livre",
                Description = "Passe pelo menos 20 minutos ao ar livre todos os dias. A luz natural e o ar fresco melhoram significativamente o humor e a energia.",
                Icon = "sun",
                Color = "#FFC107",
                Category = "Mood",
                CreatedAt = DateTime.UtcNow
            },
            new Tip
            {
                Id = Guid.NewGuid(),
                Title = "Conexão Social",
                Description = "Mantenha contato regular com amigos e familiares. Mesmo uma conversa rápida pode melhorar o humor e reduzir sentimentos de solidão.",
                Icon = "people",
                Color = "#FF9800",
                Category = "Mood",
                CreatedAt = DateTime.UtcNow
            },
            new Tip
            {
                Id = Guid.NewGuid(),
                Title = "Música que Eleva",
                Description = "Ouça suas músicas favoritas quando estiver se sentindo para baixo. A música ativa o sistema de recompensa do cérebro e libera dopamina.",
                Icon = "music",
                Color = "#9C27B0",
                Category = "Mood",
                CreatedAt = DateTime.UtcNow
            },
            new Tip
            {
                Id = Guid.NewGuid(),
                Title = "Atos de Gentileza",
                Description = "Faça algo gentil por alguém todos os dias. Pequenos atos de bondade aumentam a felicidade e criam conexões positivas.",
                Icon = "kindness",
                Color = "#4CAF50",
                Category = "Mood",
                CreatedAt = DateTime.UtcNow
            },

            // Tips de Wellness
            new Tip
            {
                Id = Guid.NewGuid(),
                Title = "Exercício Regular",
                Description = "Faça pelo menos 30 minutos de atividade física por dia. Pode ser uma caminhada, yoga, dança ou qualquer atividade que você goste. O exercício libera endorfinas e melhora o bem-estar geral.",
                Icon = "fitness",
                Color = "#4CAF50",
                Category = "Wellness",
                CreatedAt = DateTime.UtcNow
            },
            new Tip
            {
                Id = Guid.NewGuid(),
                Title = "Hidratação Adequada",
                Description = "Beba pelo menos 8 copos de água por dia. A desidratação pode causar fadiga, dores de cabeça e afetar o humor e a concentração.",
                Icon = "water",
                Color = "#00BCD4",
                Category = "Wellness",
                CreatedAt = DateTime.UtcNow
            },
            new Tip
            {
                Id = Guid.NewGuid(),
                Title = "Alimentação Balanceada",
                Description = "Priorize alimentos naturais e evite processados. Uma dieta rica em frutas, vegetais e proteínas magras fornece energia estável e melhora o humor.",
                Icon = "food",
                Color = "#8BC34A",
                Category = "Wellness",
                CreatedAt = DateTime.UtcNow
            },
            new Tip
            {
                Id = Guid.NewGuid(),
                Title = "Pausas Regulares",
                Description = "Faça pausas de 5 minutos a cada hora de trabalho. Levante-se, alongue-se, beba água. Isso previne fadiga, melhora a produtividade e reduz o risco de lesões.",
                Icon = "break",
                Color = "#FF9800",
                Category = "Wellness",
                CreatedAt = DateTime.UtcNow
            },
            new Tip
            {
                Id = Guid.NewGuid(),
                Title = "Limites Pessoais",
                Description = "Aprenda a dizer 'não' quando necessário. Estabelecer limites saudáveis protege seu bem-estar e previne sobrecarga e burnout.",
                Icon = "boundaries",
                Color = "#9E9E9E",
                Category = "Wellness",
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.Tips.AddRangeAsync(tips);
        await context.SaveChangesAsync();
    }
}

